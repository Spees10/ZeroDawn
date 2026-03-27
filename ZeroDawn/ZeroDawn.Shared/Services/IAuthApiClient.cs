#nullable enable

using ZeroDawn.Shared.Contracts.Auth;
using ZeroDawn.Shared.Contracts.Common;

namespace ZeroDawn.Shared.Services;

public interface IAuthApiClient
{
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request);
    Task<ApiResponse> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<ApiResponse> ResendConfirmationAsync(ResendConfirmationRequest request);
    Task<ApiResponse> LogoutAsync();
}
