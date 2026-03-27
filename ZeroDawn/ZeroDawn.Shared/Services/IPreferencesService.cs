#nullable enable

namespace ZeroDawn.Shared.Services;

public interface IPreferencesService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}
