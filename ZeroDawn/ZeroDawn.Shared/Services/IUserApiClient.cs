#nullable enable

using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Shared.Contracts.Users;

namespace ZeroDawn.Shared.Services;

public interface IUserApiClient
{
    Task<ApiResponse<PagedResponse<UserDto>>> GetUsersAsync(PagedRequest request);
    Task<ApiResponse<UserDto>> GetUserByIdAsync(string id);
    Task<ApiResponse<UserDto>> GetProfileAsync();
    Task<ApiResponse<UserDto>> UpdateProfileAsync(UpdateProfileRequest request);
    Task<ApiResponse<UserDto>> UpdateStatusAsync(string id, UpdateUserStatusRequest request);
    Task<ApiResponse<UserDto>> AssignRoleAsync(string id, AssignRoleRequest request);
}
