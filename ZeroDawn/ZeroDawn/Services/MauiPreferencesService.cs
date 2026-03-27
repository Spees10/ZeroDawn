#nullable enable

using Microsoft.Maui.Storage;
using ZeroDawn.Shared.Services;

namespace ZeroDawn.Services;

public class MauiPreferencesService : IPreferencesService
{
    public Task<string?> GetAsync(string key)
        => Task.FromResult(Preferences.Default.ContainsKey(key)
            ? Preferences.Default.Get<string?>(key, null)
            : null);

    public Task SetAsync(string key, string value)
    {
        Preferences.Default.Set(key, value);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        Preferences.Default.Remove(key);
        return Task.CompletedTask;
    }
}
