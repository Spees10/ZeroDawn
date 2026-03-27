#nullable enable

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
[Route("api/admin")]
[Authorize(Roles = Roles.SuperAdmin)]
[IgnoreAntiforgeryToken]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("error-logs")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ErrorLogDto>>>> GetErrorLogs([FromQuery] PagedRequest request)
    {
        try
        {
            var normalized = NormalizePagedRequest(request);
            var query = _dbContext.ErrorLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(normalized.SearchTerm))
            {
                var search = normalized.SearchTerm.Trim();
                query = query.Where(log =>
                    log.ReferenceNumber.Contains(search) ||
                    log.Message.Contains(search) ||
                    (log.RequestPath != null && log.RequestPath.Contains(search)) ||
                    (log.CorrelationId != null && log.CorrelationId.Contains(search)));
            }

            query = ApplyErrorLogSorting(query, normalized);

            var totalCount = await query.CountAsync();
            var logs = await query
                .Skip((normalized.Page - 1) * normalized.PageSize)
                .Take(normalized.PageSize)
                .Select(log => new ErrorLogDto
                {
                    Id = log.Id,
                    ReferenceNumber = log.ReferenceNumber,
                    Message = log.Message,
                    StackTrace = log.StackTrace,
                    Source = log.Source,
                    InnerException = log.InnerException,
                    UserId = log.UserId,
                    RequestPath = log.RequestPath,
                    CorrelationId = log.CorrelationId,
                    CreatedAt = log.CreatedAt
                })
                .ToListAsync();

            return Ok(Success(new PagedResponse<ErrorLogDto>
            {
                Items = logs,
                TotalCount = totalCount,
                Page = normalized.Page,
                PageSize = normalized.PageSize
            }));
        }
        catch (Exception ex)
        {
            return InternalServerError<PagedResponse<ErrorLogDto>>(ex, "Failed to retrieve error logs.");
        }
    }

    [HttpGet("admins")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAdmins()
    {
        try
        {
            var adminProjections = await _dbContext.Users
                .AsNoTracking()
                .Where(user => _dbContext.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id &&
                    _dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Name == Roles.Admin)))
                .OrderBy(user => user.FullName)
                .Select(user => new
                {
                    user.Id,
                    user.FullName,
                    Email = user.Email ?? string.Empty,
                    user.IsActive,
                    user.CreatedAt,
                    user.LastLoginAt,
                    user.EmailConfirmed
                })
                .ToListAsync();

            var adminIds = adminProjections.Select(admin => admin.Id).ToList();
            var rolesLookup = await _dbContext.UserRoles
                .Where(userRole => adminIds.Contains(userRole.UserId))
                .Join(
                    _dbContext.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => new { userRole.UserId, RoleName = role.Name ?? string.Empty })
                .GroupBy(item => item.UserId)
                .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.RoleName).OrderBy(role => role).ToList());

            var dtos = adminProjections
                .Select(admin => new UserDto
                {
                    Id = admin.Id,
                    FullName = admin.FullName,
                    Email = admin.Email,
                    Roles = rolesLookup.GetValueOrDefault(admin.Id, []),
                    IsActive = admin.IsActive,
                    CreatedAt = admin.CreatedAt,
                    LastLoginAt = admin.LastLoginAt,
                    EmailConfirmed = admin.EmailConfirmed
                })
                .ToList();

            return Ok(Success(dtos));
        }
        catch (Exception ex)
        {
            return InternalServerError<List<UserDto>>(ex, "Failed to retrieve admins.");
        }
    }

    [HttpPut("admins/{id}/role")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateAdminRole(string id, AssignRoleRequest request)
    {
        try
        {
            if (!string.Equals(id, request.UserId, StringComparison.Ordinal))
            {
                return BadRequest(Failure<UserDto>("Route user id does not match payload.", "USER_ID_MISMATCH"));
            }

            if (request.Role is not (Roles.Admin or Roles.User))
            {
                return BadRequest(Failure<UserDto>("Role must be Admin or User.", "INVALID_ROLE"));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound(Failure<UserDto>("User not found.", "USER_NOT_FOUND"));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (request.Role == Roles.Admin)
            {
                if (!currentRoles.Contains(Roles.Admin))
                {
                    var promoteResult = await _userManager.AddToRoleAsync(user, Roles.Admin);
                    if (!promoteResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ValidationFailure<UserDto>(promoteResult.Errors.Select(error => error.Description).ToList()));
                    }
                }

                if (!currentRoles.Contains(Roles.User))
                {
                    var ensureUserResult = await _userManager.AddToRoleAsync(user, Roles.User);
                    if (!ensureUserResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ValidationFailure<UserDto>(ensureUserResult.Errors.Select(error => error.Description).ToList()));
                    }
                }
            }
            else
            {
                if (currentRoles.Contains(Roles.Admin))
                {
                    var demoteResult = await _userManager.RemoveFromRoleAsync(user, Roles.Admin);
                    if (!demoteResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ValidationFailure<UserDto>(demoteResult.Errors.Select(error => error.Description).ToList()));
                    }
                }

                if (!currentRoles.Contains(Roles.User))
                {
                    var ensureUserResult = await _userManager.AddToRoleAsync(user, Roles.User);
                    if (!ensureUserResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ValidationFailure<UserDto>(ensureUserResult.Errors.Select(error => error.Description).ToList()));
                    }
                }
            }

            await transaction.CommitAsync();

            var roles = await _userManager.GetRolesAsync(user);
            var dto = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = [.. roles.OrderBy(role => role)],
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                EmailConfirmed = user.EmailConfirmed
            };

            _logger.LogInformation("Updated admin role for user: {UserId}", user.Id);
            return Ok(Success(dto));
        }
        catch (Exception ex)
        {
            return InternalServerError<UserDto>(ex, "Failed to update admin role.");
        }
    }

    private IQueryable<ErrorLog> ApplyErrorLogSorting(IQueryable<ErrorLog> query, PagedRequest request)
    {
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        var descending = request.SortDescending || string.IsNullOrWhiteSpace(sortBy);

        return sortBy switch
        {
            "referencenumber" => descending ? query.OrderByDescending(log => log.ReferenceNumber) : query.OrderBy(log => log.ReferenceNumber),
            "message" => descending ? query.OrderByDescending(log => log.Message) : query.OrderBy(log => log.Message),
            _ => descending ? query.OrderByDescending(log => log.CreatedAt) : query.OrderBy(log => log.CreatedAt)
        };
    }

    private ActionResult<ApiResponse<T>> InternalServerError<T>(Exception exception, string message)
    {
        var referenceNumber = BuildReferenceNumber("ADM");
        _logger.LogError(exception, "Admin API failure. Reference: {ReferenceNumber}", referenceNumber);

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
