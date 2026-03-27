#nullable enable

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Shared.Contracts.Users;
using ZeroDawn.Shared.Core.Constants;
using ZeroDawn.Web.Data;

namespace ZeroDawn.Web.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[IgnoreAntiforgeryToken]
public class UsersController : ControllerBase
{
    private static readonly HashSet<string> AllowedRoles =
    [
        Roles.SuperAdmin,
        Roles.Admin,
        Roles.User
    ];

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserDto>>>> GetAll([FromQuery] PagedRequest request)
    {
        try
        {
            var normalized = NormalizePagedRequest(request);
            var query = _dbContext.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(normalized.SearchTerm))
            {
                var search = normalized.SearchTerm.Trim();
                query = query.Where(user =>
                    user.FullName.Contains(search) ||
                    (user.Email != null && user.Email.Contains(search)));
            }

            query = ApplyUserSorting(query, normalized);

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((normalized.Page - 1) * normalized.PageSize)
                .Take(normalized.PageSize)
                .Select(user => new UserProjection
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    EmailConfirmed = user.EmailConfirmed
                })
                .ToListAsync();

            var userDtos = await MapUsersAsync(users);
            return Ok(Success(new PagedResponse<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = normalized.Page,
                PageSize = normalized.PageSize
            }));
        }
        catch (Exception ex)
        {
            return InternalServerError<PagedResponse<UserDto>>(ex, "Failed to retrieve users.");
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(string id)
    {
        try
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .Where(candidate => candidate.Id == id)
                .Select(candidate => new UserProjection
                {
                    Id = candidate.Id,
                    FullName = candidate.FullName,
                    Email = candidate.Email ?? string.Empty,
                    IsActive = candidate.IsActive,
                    CreatedAt = candidate.CreatedAt,
                    LastLoginAt = candidate.LastLoginAt,
                    EmailConfirmed = candidate.EmailConfirmed
                })
                .SingleOrDefaultAsync();

            if (user is null)
            {
                return NotFound(Failure<UserDto>("User not found.", "USER_NOT_FOUND"));
            }

            var dto = (await MapUsersAsync([user])).Single();
            return Ok(Success(dto));
        }
        catch (Exception ex)
        {
            return InternalServerError<UserDto>(ex, "Failed to retrieve user.");
        }
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(Failure<UserDto>("Unauthorized.", "UNAUTHORIZED"));
            }

            var user = await _dbContext.Users
                .AsNoTracking()
                .Where(candidate => candidate.Id == userId)
                .Select(candidate => new UserProjection
                {
                    Id = candidate.Id,
                    FullName = candidate.FullName,
                    Email = candidate.Email ?? string.Empty,
                    IsActive = candidate.IsActive,
                    CreatedAt = candidate.CreatedAt,
                    LastLoginAt = candidate.LastLoginAt,
                    EmailConfirmed = candidate.EmailConfirmed
                })
                .SingleOrDefaultAsync();

            if (user is null)
            {
                return NotFound(Failure<UserDto>("User not found.", "USER_NOT_FOUND"));
            }

            var dto = (await MapUsersAsync([user])).Single();
            return Ok(Success(dto));
        }
        catch (Exception ex)
        {
            return InternalServerError<UserDto>(ex, "Failed to retrieve profile.");
        }
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile(UpdateProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(Failure<UserDto>("Unauthorized.", "UNAUTHORIZED"));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return NotFound(Failure<UserDto>("User not found.", "USER_NOT_FOUND"));
            }

            user.FullName = request.FullName.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(ValidationFailure<UserDto>(updateResult.Errors.Select(error => error.Description).ToList()));
            }

            var dto = await BuildUserDtoAsync(user);
            _logger.LogInformation("Updated profile for user: {UserId}", user.Id);

            return Ok(Success(dto));
        }
        catch (Exception ex)
        {
            return InternalServerError<UserDto>(ex, "Failed to update profile.");
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateStatus(string id, UpdateUserStatusRequest request)
    {
        try
        {
            if (!string.Equals(id, request.UserId, StringComparison.Ordinal))
            {
                return BadRequest(Failure<UserDto>("Route user id does not match payload.", "USER_ID_MISMATCH"));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound(Failure<UserDto>("User not found.", "USER_NOT_FOUND"));
            }

            if (string.Equals(user.Id, GetCurrentUserId(), StringComparison.Ordinal) && !request.IsActive)
            {
                return BadRequest(Failure<UserDto>("You cannot disable your own account.", "SELF_DISABLE_NOT_ALLOWED"));
            }

            user.IsActive = request.IsActive;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(ValidationFailure<UserDto>(updateResult.Errors.Select(error => error.Description).ToList()));
            }

            var dto = await BuildUserDtoAsync(user);
            _logger.LogInformation("Updated active status for user: {UserId}", user.Id);

            return Ok(Success(dto));
        }
        catch (Exception ex)
        {
            return InternalServerError<UserDto>(ex, "Failed to update user status.");
        }
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<ApiResponse<UserDto>>> AssignRole(string id, AssignRoleRequest request)
    {
        try
        {
            if (!string.Equals(id, request.UserId, StringComparison.Ordinal))
            {
                return BadRequest(Failure<UserDto>("Route user id does not match payload.", "USER_ID_MISMATCH"));
            }

            if (!AllowedRoles.Contains(request.Role))
            {
                return BadRequest(Failure<UserDto>("Invalid role.", "INVALID_ROLE"));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound(Failure<UserDto>("User not found.", "USER_NOT_FOUND"));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var existingRoles = await _userManager.GetRolesAsync(user);
            if (existingRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
                if (!removeResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(ValidationFailure<UserDto>(removeResult.Errors.Select(error => error.Description).ToList()));
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, request.Role);
            if (!addResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(ValidationFailure<UserDto>(addResult.Errors.Select(error => error.Description).ToList()));
            }

            await transaction.CommitAsync();

            var dto = await BuildUserDtoAsync(user);
            _logger.LogInformation("Assigned role for user: {UserId}", user.Id);

            return Ok(Success(dto));
        }
        catch (Exception ex)
        {
            return InternalServerError<UserDto>(ex, "Failed to assign role.");
        }
    }

    private IQueryable<ApplicationUser> ApplyUserSorting(IQueryable<ApplicationUser> query, PagedRequest request)
    {
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        var descending = request.SortDescending;

        return sortBy switch
        {
            "fullname" => descending ? query.OrderByDescending(user => user.FullName) : query.OrderBy(user => user.FullName),
            "email" => descending ? query.OrderByDescending(user => user.Email) : query.OrderBy(user => user.Email),
            "lastloginat" => descending ? query.OrderByDescending(user => user.LastLoginAt) : query.OrderBy(user => user.LastLoginAt),
            _ => descending ? query.OrderByDescending(user => user.CreatedAt) : query.OrderBy(user => user.CreatedAt)
        };
    }

    private async Task<List<UserDto>> MapUsersAsync(IReadOnlyCollection<UserProjection> users)
    {
        if (users.Count == 0)
        {
            return [];
        }

        var userIds = users.Select(user => user.Id).ToList();
        var rolesLookup = await _dbContext.UserRoles
            .Where(userRole => userIds.Contains(userRole.UserId))
            .Join(
                _dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, RoleName = role.Name ?? string.Empty })
            .GroupBy(item => item.UserId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.RoleName).OrderBy(name => name).ToList());

        return users
            .Select(user => new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = rolesLookup.GetValueOrDefault(user.Id, []),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                EmailConfirmed = user.EmailConfirmed
            })
            .ToList();
    }

    private async Task<UserDto> BuildUserDtoAsync(ApplicationUser user)
    {
        var projection = new UserProjection
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            EmailConfirmed = user.EmailConfirmed
        };

        return (await MapUsersAsync([projection])).Single();
    }

    private string? GetCurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

    private ActionResult<ApiResponse<T>> InternalServerError<T>(Exception exception, string message)
    {
        var referenceNumber = BuildReferenceNumber("USR");
        _logger.LogError(exception, "Users API failure. Reference: {ReferenceNumber}", referenceNumber);

        return StatusCode(
            StatusCodes.Status500InternalServerError,
            new ApiResponse<T>
            {
                Succeeded = false,
                Error = message,
                ErrorCode = "SERVER_ERROR",
                ReferenceNumber = referenceNumber
            });
    }

    private string BuildReferenceNumber(string prefix)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N");
        var suffixLength = Math.Min(6, correlationId.Length);
        return $"{prefix}-{DateTime.UtcNow:MMdd}-{correlationId[..suffixLength]}";
    }

    private static ApiResponse<T> Success<T>(T data)
        => new()
        {
            Succeeded = true,
            Data = data
        };

    private static ApiResponse<T> Failure<T>(string error, string? errorCode = null)
        => new()
        {
            Succeeded = false,
            Error = error,
            ErrorCode = errorCode
        };

    private static ApiResponse<T> ValidationFailure<T>(List<string> errors)
        => new()
        {
            Succeeded = false,
            Error = "Validation failed.",
            ErrorCode = "VALIDATION_ERROR",
            ValidationErrors = errors
        };

    private sealed class UserProjection
    {
        public string Id { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public bool EmailConfirmed { get; init; }
    }

    private static PagedRequest NormalizePagedRequest(PagedRequest request)
        => new()
        {
            Page = request.Page <= 0 ? 1 : request.Page,
            PageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100),
            SearchTerm = request.SearchTerm,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };
}
