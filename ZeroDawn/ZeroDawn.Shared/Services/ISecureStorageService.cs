#nullable enable

namespace ZeroDawn.Shared.Services;

public interface ISecureStorageService
{
    Task<string?> GetSecureAsync(string key);
    Task SetSecureAsync(string key, string value);
    Task RemoveSecureAsync(string key);
}
