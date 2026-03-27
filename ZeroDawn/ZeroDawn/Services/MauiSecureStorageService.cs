#nullable enable

using Microsoft.Maui.Storage;
using ZeroDawn.Shared.Services;

namespace ZeroDawn.Services;

public class MauiSecureStorageService : ISecureStorageService
{
    public async Task<string?> GetSecureAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetSecureAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
        }
        catch
        {
        }
    }

    public Task RemoveSecureAsync(string key)
    {
        try
        {
            SecureStorage.Default.Remove(key);
        }
        catch
        {
        }

        return Task.CompletedTask;
    }
}
