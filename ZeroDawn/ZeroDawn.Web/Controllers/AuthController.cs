#nullable enable

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroDawn.Shared.Contracts.Auth;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Shared.Core.Constants;
using ZeroDawn.Web.Configuration;
using ZeroDawn.Web.Data;
using ZeroDawn.Web.Services;

namespace ZeroDawn.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _dbContext;
    private readonly AppOptions _appOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ApplicationDbContext dbContext,
        IOptions<AppOptions> appOptions,
        IOptions<JwtOptions> jwtOptions,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _appOptions = appOptions.Value;
        _jwtOptions = jwtOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Unauthorized(Failure<AuthResponse>("Invalid credentials."));
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return Unauthorized(Failure<AuthResponse>("Invalid credentials."));
            }

            if (!user.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden, Failure<AuthResponse>("Account is disabled."));
            }

            if (_appOptions.RequireEmailConfirmation && !user.EmailConfirmed)
            {
                return StatusCode(StatusCodes.Status403Forbidden, Failure<AuthResponse>("Email not confirmed."));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = HashToken(refreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays);
            user.LastLoginAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    Failure<AuthResponse>("Failed to update authentication state."));
            }

            _logger.LogInformation("User logged in: {UserId}", user.Id);

            return Ok(Success(CreateAuthResponse(user, roles, accessToken, refreshToken)));
        }
        catch (Exception ex)
        {
            return InternalServerError(ex, "An error occurred during login.");
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request)
    {
        try
        {
            if (!_appOptions.AllowSelfRegistration)
            {
                return StatusCode(StatusCodes.Status403Forbidden, Failure<AuthResponse>("Self-registration is disabled."));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(ValidationFailure<AuthResponse>(createResult.Errors.Select(e => e.Description).ToList()));
            }

            var addToRoleResult = await _userManager.AddToRoleAsync(user, Roles.User);
            if (!addToRoleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(ValidationFailure<AuthResponse>(addToRoleResult.Errors.Select(e => e.Description).ToList()));
            }

            if (_appOptions.RequireEmailConfirmation)
            {
                _ = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                await transaction.CommitAsync();

                _logger.LogInformation("Registered user pending email confirmation: {UserId}", user.Id);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Succeeded = true,
                    Message = "Please confirm your email."
                });
            }

            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmResult = await _userManager.ConfirmEmailAsync(user, confirmationToken);
            if (!confirmResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(ValidationFailure<AuthResponse>(confirmResult.Errors.Select(e => e.Description).ToList()));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = HashToken(refreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays);
            user.LastLoginAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(ValidationFailure<AuthResponse>(updateResult.Errors.Select(e => e.Description).ToList()));
            }

            await transaction.CommitAsync();

            _logger.LogInformation("User registered: {UserId}", user.Id);

            return Ok(Success(CreateAuthResponse(user, roles, accessToken, refreshToken)));
        }
        catch (Exception ex)
        {
            return InternalServerError(ex, "An error occurred during registration.");
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshTokenRequest request)
    {
        try
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal is null)
            {
                return Unauthorized(Failure<AuthResponse>("Invalid token."));
            }

            var userId = GetUserId(principal);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(Failure<AuthResponse>("Invalid token."));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Unauthorized(Failure<AuthResponse>("Invalid token."));
            }

            if (string.IsNullOrWhiteSpace(user.RefreshToken) ||
                user.RefreshTokenExpiryTime is null ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow ||
                !RefreshTokenMatches(user.RefreshToken, request.RefreshToken))
            {
                return Unauthorized(Failure<AuthResponse>("Invalid token."));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = HashToken(refreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(ValidationFailure<AuthResponse>(updateResult.Errors.Select(e => e.Description).ToList()));
            }

            _logger.LogInformation("Refreshed token for user: {UserId}", user.Id);

            return Ok(Success(CreateAuthResponse(user, roles, accessToken, refreshToken)));
        }
        catch (Exception ex)
        {
            return InternalServerError(ex, "An error occurred during token refresh.");
        }
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword(ForgotPasswordRequest request)
    {
        const string message = "If the email exists, a reset link has been sent.";

        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is not null && user.IsActive)
            {
                _ = await _userManager.GeneratePasswordResetTokenAsync(user);

                if (_environment.IsDevelopment())
                {
                    _logger.LogInformation("Generated password reset token for user: {UserId}", user.Id);
                }
            }

            return Ok(SuccessMessage(message));
        }
        catch (Exception ex)
        {
            return InternalServerError(ex, "An error occurred during forgot-password processing.");
        }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword(ResetPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return BadRequest(ValidationFailure<string>(["Invalid email or token."]));
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(ValidationFailure<string>(result.Errors.Select(e => e.Description).ToList()));
            }

            _logger.LogInformation("Password reset completed for user: {UserId}", user.Id);

            return Ok(SuccessMessage("Password has been reset successfully."));
        }
        catch (Exception ex)
        {
            return InternalServerError(ex, "An error occurred during password reset.");
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword(ChangePasswordRequest request)
    {
        try
        {
            var userId = GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(Failure<string>("Unauthorized."));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Unauthorized(Failure<string>("Unauthorized."));
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(ValidationFailure<string>(result.Errors.Select(e => e.Description).ToList()));
            }

            _logger.LogInformation("Password changed for user: {UserId}", user.Id);

            return Ok(SuccessMessage("Password changed successfully."));
        }
        catch (Exception ex)
        {
            return InternalServerError(ex, "An error occurred during password change.");
        }
    }

    [HttpPost("confirm-email")]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmEmail(ConfirmEmailRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null)
            {
                return BadRequest(ValidationFailure<string>(["Invalid confirmation request."]));
            }

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
            {
                return BadRequest(ValidationFailure<string>(result.Errors.Select(e => e.Description).ToList()));
            }

            _logger.LogInformation("Email confirmed for user: {UserId}", user.Id);

            return Ok(SuccessMessage("Email confirmed successfully."));
        }
        catch (Exception ex)
        {
            return InternalServerError(ex, "An error occurred during email confirmation.");
        }
    }

    [HttpPost("resend-confirmation")]
    public async Task<ActionResult<ApiResponse<string>>> ResendConfirmation(ResendConfirmationRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is not null && !user.EmailConfirmed)
            {
                _ = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                if (_environment.IsDevelopment())
                {
                    _logger.LogInformation("Generated email confirmation token for user: {UserId}", user.Id);
                }
            }

            return Ok(SuccessMessage("If the account exists, a confirmation email will be sent."));
        }
        catch (Exception ex)
        {
            return InternalServerError(ex, "An error occurred while resending confirmation.");
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<string>>> Logout()
    {
        try
        {
            var userId = GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(Failure<string>("Unauthorized."));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Unauthorized(Failure<string>("Unauthorized."));
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(ValidationFailure<string>(result.Errors.Select(e => e.Description).ToList()));
            }

            _logger.LogInformation("User logged out: {UserId}", user.Id);

            return Ok(SuccessMessage("Logged out successfully."));
        }
        catch (Exception ex)
        {
            return InternalServerError(ex, "An error occurred during logout.");
        }
    }

    private AuthResponse CreateAuthResponse(
        ApplicationUser user,
        IList<string> roles,
        string accessToken,
        string refreshToken)
        => new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = [.. roles]
        };

    private ActionResult InternalServerError(Exception ex, string message)
    {
        var referenceNumber = CreateReferenceNumber();
        _logger.LogError(ex, "{Message} ReferenceNumber: {ReferenceNumber}", message, referenceNumber);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = $"Reference number: {referenceNumber}"
        };

        problem.Extensions["referenceNumber"] = referenceNumber;

        return StatusCode(StatusCodes.Status500InternalServerError, problem);
    }

    private static ApiResponse<T> Success<T>(T data, string? message = null)
        => new()
        {
            Succeeded = true,
            Data = data,
            Message = message
        };

    private static ApiResponse<string> SuccessMessage(string message)
        => new()
        {
            Succeeded = true,
            Message = message
        };

    private static ApiResponse<T> Failure<T>(string error, string? errorCode = null)
        => new()
        {
            Succeeded = false,
            Error = error,
            ErrorCode = errorCode
        };

    private static ApiResponse<T> ValidationFailure<T>(List<string> validationErrors)
        => new()
        {
            Succeeded = false,
            Error = "Validation failed.",
            ErrorCode = "VALIDATION_ERROR",
            ValidationErrors = validationErrors
        };

    private static string CreateReferenceNumber()
        => Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool RefreshTokenMatches(string storedHash, string refreshToken)
    {
        var candidateHash = HashToken(refreshToken);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(storedHash),
            Convert.FromHexString(candidateHash));
    }

    private static string? GetUserId(ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
}
