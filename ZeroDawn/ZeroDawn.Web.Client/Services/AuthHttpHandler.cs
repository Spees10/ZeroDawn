#nullable enable

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ZeroDawn.Shared.Contracts.Auth;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Shared.Core;
using ZeroDawn.Shared.Services;

namespace ZeroDawn.Web.Client.Services;

public class AuthHttpHandler : DelegatingHandler
{
    private const string RefreshClientName = "ApiNoAuth";
    private static readonly HttpRequestOptionsKey<bool> RetryAttemptedKey = new("AuthRetryAttempted");

    private readonly ITokenStorageService _tokenStorageService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebAuthStateProvider _authStateProvider;

    public AuthHttpHandler(
        ITokenStorageService tokenStorageService,
        IHttpClientFactory httpClientFactory,
        WebAuthStateProvider authStateProvider)
    {
        _tokenStorageService = tokenStorageService;
        _httpClientFactory = httpClientFactory;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var retryRequest = await CloneRequestAsync(request, cancellationToken);
        var accessToken = await _tokenStorageService.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || HasRetried(request) || IsRefreshRequest(request))
        {
            return response;
        }

        response.Dispose();

        var refreshed = await TryRefreshTokenAsync(accessToken, cancellationToken);
        if (!refreshed)
        {
            await _tokenStorageService.ClearTokensAsync();
            _authStateProvider.NotifyAuthStateChanged();

            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request
            };
        }

        var newAccessToken = await _tokenStorageService.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(newAccessToken))
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request
            };
        }

        retryRequest.Options.Set(RetryAttemptedKey, true);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task<bool> TryRefreshTokenAsync(string? accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var refreshToken = await _tokenStorageService.GetRefreshTokenAsync();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var client = _httpClientFactory.CreateClient(RefreshClientName);
        var response = await client.PostAsJsonAsync(
            ApiEndpoints.Auth.RefreshToken,
            new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(cancellationToken: cancellationToken);
        if (payload is null ||
            !payload.Succeeded ||
            payload.Data is null ||
            string.IsNullOrWhiteSpace(payload.Data.AccessToken) ||
            string.IsNullOrWhiteSpace(payload.Data.RefreshToken))
        {
            return false;
        }

        await _tokenStorageService.SetTokensAsync(payload.Data.AccessToken, payload.Data.RefreshToken);
        _authStateProvider.NotifyAuthStateChanged();

        return true;
    }

    private static bool HasRetried(HttpRequestMessage request)
        => request.Options.TryGetValue(RetryAttemptedKey, out var retried) && retried;

    private static bool IsRefreshRequest(HttpRequestMessage request)
        => string.Equals(
            request.RequestUri?.AbsolutePath.TrimStart('/'),
            ApiEndpoints.Auth.RefreshToken,
            StringComparison.OrdinalIgnoreCase);

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        if (request.Content is not null)
        {
            var memoryStream = new MemoryStream();
            await request.Content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            clone.Content = new StreamContent(memoryStream);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
