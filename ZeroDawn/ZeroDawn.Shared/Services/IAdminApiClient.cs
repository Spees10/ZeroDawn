#nullable enable

using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Shared.Contracts.Users;

namespace ZeroDawn.Shared.Services;

public interface IAdminApiClient
{
    Task<ApiResponse<PagedResponse<ErrorLogDto>>> GetErrorLogsAsync(PagedRequest request);
    Task<ApiResponse<List<UserDto>>> GetAdminsAsync();
    Task<ApiResponse<UserDto>> UpdateAdminRoleAsync(string id, AssignRoleRequest request);
}
