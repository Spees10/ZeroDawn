#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using ZeroDawn.Shared.Contracts.Auth;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Shared.Core;

namespace ZeroDawn.Shared.Services;

public class DefaultAuthApiClient : IAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly IStringLocalizer<SharedResources> localizer;

    public DefaultAuthApiClient(HttpClient httpClient, IStringLocalizer<SharedResources> localizer)
    {
        this.httpClient = httpClient;
        this.localizer = localizer;
    }

    public Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        => PostAuthAsync(ApiEndpoints.Auth.Login, request);

    public Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        => PostAuthAsync(ApiEndpoints.Auth.Register, request);

    public Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        => PostAuthAsync(ApiEndpoints.Auth.RefreshToken, request);

    public Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        => PostAsync(ApiEndpoints.Auth.ForgotPassword, request);

    public Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
        => PostAsync(ApiEndpoints.Auth.ResetPassword, request);

    public Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request)
        => PostAsync(ApiEndpoints.Auth.ChangePassword, request);

    public Task<ApiResponse> ConfirmEmailAsync(ConfirmEmailRequest request)
        => PostAsync(ApiEndpoints.Auth.ConfirmEmail, request);

    public Task<ApiResponse> ResendConfirmationAsync(ResendConfirmationRequest request)
        => PostAsync(ApiEndpoints.Auth.ResendConfirmation, request);

    public Task<ApiResponse> LogoutAsync()
        => PostAsync<object?>(ApiEndpoints.Auth.Logout, null);

    private async Task<ApiResponse<AuthResponse>> PostAuthAsync<TRequest>(string path, TRequest request)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(path, request, JsonOptions);
            return await ReadAuthResponseAsync(response);
        }
        catch (HttpRequestException)
        {
            return BuildNetworkFailure<AuthResponse>();
        }
    }

    private async Task<ApiResponse> PostAsync<TRequest>(string path, TRequest request)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(path, request, JsonOptions);
            return await ReadResponseAsync(response);
        }
        catch (HttpRequestException)
        {
            return BuildNetworkFailure();
        }
    }

    private async Task<ApiResponse<AuthResponse>> ReadAuthResponseAsync(HttpResponseMessage response)
    {
        var payload = await TryReadAsync<ApiResponse<AuthResponse>>(response);

        if (payload is not null)
        {
            return payload;
        }

        return response.IsSuccessStatusCode
            ? new ApiResponse<AuthResponse>
            {
                Succeeded = false,
                Error = localizer["EmptyResponse"],
                ErrorCode = "EMPTY_RESPONSE"
            }
            : BuildStatusFailure<AuthResponse>(response);
    }

    private async Task<ApiResponse> ReadResponseAsync(HttpResponseMessage response)
    {
        var payload = await TryReadAsync<ApiResponse>(response);

        if (payload is not null)
        {
            return payload;
        }

        return response.IsSuccessStatusCode
            ? new ApiResponse
            {
                Succeeded = false,
                Error = localizer["EmptyResponse"],
                ErrorCode = "EMPTY_RESPONSE"
            }
            : BuildStatusFailure(response);
    }

    private static async Task<T?> TryReadAsync<T>(HttpResponseMessage response)
    {
        if (response.Content is null)
        {
            return default;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
        catch (NotSupportedException)
        {
            return default;
        }
    }

    private ApiResponse BuildNetworkFailure()
        => new()
        {
            Succeeded = false,
            Error = localizer["NetworkError"],
            ErrorCode = "NETWORK_ERROR"
        };

    private ApiResponse<T> BuildNetworkFailure<T>()
        => new()
        {
            Succeeded = false,
            Error = localizer["NetworkError"],
            ErrorCode = "NETWORK_ERROR"
        };

    private ApiResponse BuildStatusFailure(HttpResponseMessage response)
        => new()
        {
            Succeeded = false,
            Error = localizer["RequestFailedWithStatus", (int)response.StatusCode],
            ErrorCode = $"HTTP_{(int)response.StatusCode}"
        };

    private ApiResponse<T> BuildStatusFailure<T>(HttpResponseMessage response)
        => new()
        {
            Succeeded = false,
            Error = localizer["RequestFailedWithStatus", (int)response.StatusCode],
            ErrorCode = $"HTTP_{(int)response.StatusCode}"
        };
}
