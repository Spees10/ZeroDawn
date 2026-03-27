#nullable enable

using System.Reflection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ZeroDawn.Shared.Contracts.Auth;
using ZeroDawn.Shared.Contracts.Common;

namespace ZeroDawn.Shared.Services;

public class DefaultAuthService : IAuthService
{
    private readonly IAuthApiClient authApiClient;
    private readonly IServiceProvider services;

    public DefaultAuthService(IAuthApiClient authApiClient, IServiceProvider services)
    {
        this.authApiClient = authApiClient;
        this.services = services;
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var response = await authApiClient.LoginAsync(request);
        await PersistTokensIfPresentAsync(response);
        return response;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var response = await authApiClient.RegisterAsync(request);
        await PersistTokensIfPresentAsync(response);
        return response;
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync()
    {
        var tokenStorage = services.GetService<ITokenStorageService>();
        if (tokenStorage is null)
        {
            return ServiceUnavailable<AuthResponse>("Token storage is not available.");
        }

        var accessToken = await tokenStorage.GetAccessTokenAsync();
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Failure<AuthResponse>("Missing stored authentication tokens.", "MISSING_TOKENS");
        }

        var response = await authApiClient.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });

        await PersistTokensIfPresentAsync(response);
        return response;
    }

    public Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        => authApiClient.ForgotPasswordAsync(request);

    public Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
        => authApiClient.ResetPasswordAsync(request);

    public Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request)
        => authApiClient.ChangePasswordAsync(request);

    public Task<ApiResponse> ConfirmEmailAsync(ConfirmEmailRequest request)
        => authApiClient.ConfirmEmailAsync(request);

    public Task<ApiResponse> ResendConfirmationAsync(ResendConfirmationRequest request)
        => authApiClient.ResendConfirmationAsync(request);

    public async Task LogoutAsync()
    {
        try
        {
            await authApiClient.LogoutAsync();
        }
        catch
        {
        }

        var tokenStorage = services.GetService<ITokenStorageService>();
        if (tokenStorage is not null)
        {
            await tokenStorage.ClearTokensAsync();
        }

        await NotifyAuthStateChangedAsync();
    }

    private async Task PersistTokensIfPresentAsync(ApiResponse<AuthResponse> response)
    {
        if (!response.Succeeded ||
            response.Data is null ||
            string.IsNullOrWhiteSpace(response.Data.AccessToken) ||
            string.IsNullOrWhiteSpace(response.Data.RefreshToken))
        {
            return;
        }

        var tokenStorage = services.GetService<ITokenStorageService>();
        if (tokenStorage is not null)
        {
            await tokenStorage.SetTokensAsync(response.Data.AccessToken, response.Data.RefreshToken);
        }

        await NotifyAuthStateChangedAsync();
    }

    private async Task NotifyAuthStateChangedAsync()
    {
        var authenticationStateProvider = services.GetService<AuthenticationStateProvider>();
        if (authenticationStateProvider is null)
        {
            return;
        }

        var method = authenticationStateProvider.GetType().GetMethod(
            "NotifyAuthStateChanged",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (method is null)
        {
            return;
        }

        await Task.Run(() => method.Invoke(authenticationStateProvider, null));
    }

    private static ApiResponse<T> Failure<T>(string error, string? errorCode = null)
        => new()
        {
            Succeeded = false,
            Error = error,
            ErrorCode = errorCode
        };

    private static ApiResponse<T> ServiceUnavailable<T>(string error)
        => Failure<T>(error, "SERVICE_UNAVAILABLE");
}
