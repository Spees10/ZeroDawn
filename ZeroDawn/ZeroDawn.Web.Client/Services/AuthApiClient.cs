#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using ZeroDawn.Shared.Contracts.Auth;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Shared.Core;
using ZeroDawn.Shared.Services;

namespace ZeroDawn.Web.Client.Services;

public class AuthApiClient : IAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
            using var response = await _httpClient.PostAsJsonAsync(path, request, JsonOptions);
            return await ReadAuthResponseAsync(response);
        }
        catch (HttpRequestException)
        {
            return NetworkFailure<AuthResponse>();
        }
    }

    private async Task<ApiResponse> PostAsync<TRequest>(string path, TRequest request)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(path, request, JsonOptions);
            return await ReadResponseAsync(response);
        }
        catch (HttpRequestException)
        {
            return NetworkFailure();
        }
    }

    private static async Task<ApiResponse<AuthResponse>> ReadAuthResponseAsync(HttpResponseMessage response)
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
                Error = "The server returned an empty response.",
                ErrorCode = "EMPTY_RESPONSE"
            }
            : StatusFailure<AuthResponse>(response);
    }

    private static async Task<ApiResponse> ReadResponseAsync(HttpResponseMessage response)
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
                Error = "The server returned an empty response.",
                ErrorCode = "EMPTY_RESPONSE"
            }
            : StatusFailure(response);
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

    private static ApiResponse NetworkFailure()
        => new()
        {
            Succeeded = false,
            Error = "Network error. Please check your connection.",
            ErrorCode = "NETWORK_ERROR"
        };

    private static ApiResponse<T> NetworkFailure<T>()
        => new()
        {
            Succeeded = false,
            Error = "Network error. Please check your connection.",
            ErrorCode = "NETWORK_ERROR"
        };

    private static ApiResponse StatusFailure(HttpResponseMessage response)
        => new()
        {
            Succeeded = false,
            Error = $"Request failed with status code {(int)response.StatusCode}.",
            ErrorCode = $"HTTP_{(int)response.StatusCode}"
        };

    private static ApiResponse<T> StatusFailure<T>(HttpResponseMessage response)
        => new()
        {
            Succeeded = false,
            Error = $"Request failed with status code {(int)response.StatusCode}.",
            ErrorCode = $"HTTP_{(int)response.StatusCode}"
        };
}
