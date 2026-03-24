#nullable enable

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using ZeroDawn.Shared.Services;

namespace ZeroDawn.Services;

public class MauiAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState AnonymousState =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly ITokenStorageService _tokenStorageService;

    public MauiAuthStateProvider(ITokenStorageService tokenStorageService)
    {
        _tokenStorageService = tokenStorageService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStorageService.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AnonymousState;
        }

        try
        {
            var claims = ParseClaims(token);
            var expiry = GetExpiry(claims);
            if (expiry is null || expiry <= DateTime.UtcNow)
            {
                return AnonymousState;
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return AnonymousState;
        }
    }

    public void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static DateTime? GetExpiry(IEnumerable<Claim> claims)
    {
        var exp = claims.FirstOrDefault(claim => claim.Type == "exp")?.Value;
        if (!long.TryParse(exp, out var seconds))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
    }

    private static List<Claim> ParseClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            throw new FormatException("Invalid JWT.");
        }

        var payloadBytes = DecodeBase64Url(parts[1]);
        using var document = JsonDocument.Parse(payloadBytes);

        var claims = new List<Claim>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.Value.EnumerateArray())
                {
                    claims.Add(new Claim(property.Name, item.ToString()));
                }

                continue;
            }

            claims.Add(new Claim(property.Name, property.Value.ToString()));
        }

        return claims;
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');

        return Convert.FromBase64String(padded);
    }
}
