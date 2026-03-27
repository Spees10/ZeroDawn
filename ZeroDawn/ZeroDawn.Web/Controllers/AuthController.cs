#nullable enable

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
[IgnoreAntiforgeryToken]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _dbContext;
    private readonly AppOptions _appOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ITokenService tokenService,
        ApplicationDbContext dbContext,
        IOptions<AppOptions> appOptions,
        IOptions<JwtOptions> jwtOptions,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _appOptions = appOptions.Value;
        _jwtOptions = jwtOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    [EnableRateLimiting("auth-strict")]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
    {
        var jwtUnavailable = EnsureJwtConfigured<AuthResponse>();
        if (jwtUnavailable is not null)
        {
            return jwtUnavailable;
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(Failure<AuthResponse>("Invalid credentials."));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Locked out user attempted login: {UserId}", user.Id);
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                Failure<AuthResponse>("Account is temporarily locked. Please try again later."));
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(Failure<AuthResponse>("Invalid credentials."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        if (!user.IsActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, Failure<AuthResponse>("تم تعطيل الحساب."));
        }

        if (_appOptions.RequireEmailConfirmation && !user.EmailConfirmed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, Failure<AuthResponse>("البريد الإلكتروني غير مؤكد."));
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
                Failure<AuthResponse>("تعذر تحديث حالة المصادقة."));
        }

        _logger.LogInformation("User logged in: {UserId}", user.Id);

        return Ok(Success(CreateAuthResponse(user, roles, accessToken, refreshToken)));
    }

    [EnableRateLimiting("auth-strict")]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request)
    {
        if (!_appOptions.AllowSelfRegistration)
        {
            return StatusCode(StatusCodes.Status403Forbidden, Failure<AuthResponse>("إنشاء الحسابات الجديدة غير متاح."));
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
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var confirmationLink = $"{_appOptions.BaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";
            try
            {
                await _emailService.SendConfirmationEmailAsync(user.Email!, user.FullName, confirmationLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email");
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Registered user pending email confirmation: {UserId}", user.Id);

            return Ok(new ApiResponse<AuthResponse>
            {
                Succeeded = true,
                Message = "من فضلك أكد بريدك الإلكتروني."
            });
        }

        var jwtUnavailable = EnsureJwtConfigured<AuthResponse>();
        if (jwtUnavailable is not null)
        {
            await transaction.RollbackAsync();
            return jwtUnavailable;
        }

        var autoConfirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmResult = await _userManager.ConfirmEmailAsync(user, autoConfirmToken);
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

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshTokenRequest request)
    {
        var jwtUnavailable = EnsureJwtConfigured<AuthResponse>();
        if (jwtUnavailable is not null)
        {
            return jwtUnavailable;
        }

        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
        {
            return Unauthorized(Failure<AuthResponse>("الرمز غير صالح."));
        }

        var userId = GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(Failure<AuthResponse>("الرمز غير صالح."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized(Failure<AuthResponse>("الرمز غير صالح."));
        }

        if (string.IsNullOrWhiteSpace(user.RefreshToken) ||
            user.RefreshTokenExpiryTime is null ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow ||
            !RefreshTokenMatches(user.RefreshToken, request.RefreshToken))
        {
            return Unauthorized(Failure<AuthResponse>("الرمز غير صالح."));
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

    [EnableRateLimiting("auth-strict")]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword(ForgotPasswordRequest request)
    {
        const string message = "إذا كان البريد موجودًا، فقد تم إرسال رابط إعادة التعيين.";

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var encodedEmail = Uri.EscapeDataString(request.Email);
            var resetLink = $"{_appOptions.BaseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email!, user.FullName, resetLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email");
            }
        }

        return Ok(SuccessMessage(message));
    }

    [EnableRateLimiting("auth-strict")]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return BadRequest(ValidationFailure<string>(["البريد الإلكتروني أو الرمز غير صالح."]));
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(ValidationFailure<string>(result.Errors.Select(e => e.Description).ToList()));
        }

        _logger.LogInformation("Password reset completed for user: {UserId}", user.Id);

        return Ok(SuccessMessage("تمت إعادة تعيين كلمة المرور بنجاح."));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword(ChangePasswordRequest request)
    {
        var jwtUnavailable = EnsureJwtConfigured<string>();
        if (jwtUnavailable is not null)
        {
            return jwtUnavailable;
        }

        var userId = GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(Failure<string>("غير مصرح."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized(Failure<string>("غير مصرح."));
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(ValidationFailure<string>(result.Errors.Select(e => e.Description).ToList()));
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Invalidated refresh tokens after password change for user: {UserId}", user.Id);
        _logger.LogInformation("Password changed for user: {UserId}", user.Id);

        return Ok(SuccessMessage("تم تغيير كلمة المرور بنجاح."));
    }

    [EnableRateLimiting("auth-resend")]
    [HttpPost("confirm-email")]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmEmail(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return BadRequest(ValidationFailure<string>(["طلب تأكيد البريد غير صالح."]));
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return BadRequest(ValidationFailure<string>(result.Errors.Select(e => e.Description).ToList()));
        }

        _logger.LogInformation("Email confirmed for user: {UserId}", user.Id);

        return Ok(SuccessMessage("تم تأكيد البريد الإلكتروني بنجاح."));
    }

    [EnableRateLimiting("auth-resend")]
    [HttpPost("resend-confirmation")]
    public async Task<ActionResult<ApiResponse<string>>> ResendConfirmation(ResendConfirmationRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var confirmationLink = $"{_appOptions.BaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";
            try
            {
                await _emailService.SendConfirmationEmailAsync(user.Email!, user.FullName, confirmationLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email");
            }
        }

        return Ok(SuccessMessage("إذا كان الحساب موجودًا، فسيتم إرسال رسالة تأكيد."));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<string>>> Logout()
    {
        var jwtUnavailable = EnsureJwtConfigured<string>();
        if (jwtUnavailable is not null)
        {
            return jwtUnavailable;
        }

        var userId = GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(Failure<string>("غير مصرح."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized(Failure<string>("غير مصرح."));
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(ValidationFailure<string>(result.Errors.Select(e => e.Description).ToList()));
        }

        _logger.LogInformation("User logged out: {UserId}", user.Id);

        return Ok(SuccessMessage("تم تسجيل الخروج بنجاح."));
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
            Error = "فشل التحقق من البيانات.",
            ErrorCode = "VALIDATION_ERROR",
            ValidationErrors = validationErrors
        };

    private ActionResult<ApiResponse<T>>? EnsureJwtConfigured<T>()
    {
        if (!string.IsNullOrWhiteSpace(_jwtOptions.Secret))
        {
            return null;
        }

        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            Failure<T>("المصادقة غير مهيأة بعد.", "AUTH_NOT_CONFIGURED"));
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool RefreshTokenMatches(string storedHash, string refreshToken)
    {
        var candidateHash = HashToken(refreshToken);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(storedHash),
            Convert.FromHexString(candidateHash));
    }

    private string BuildConfirmationLink(string userId, string token)
        => $"{_appOptions.BaseUrl.TrimEnd('/')}/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";

    private string BuildResetPasswordLink(string email, string token)
        => $"{_appOptions.BaseUrl.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

    private static string? GetUserId(ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
}
