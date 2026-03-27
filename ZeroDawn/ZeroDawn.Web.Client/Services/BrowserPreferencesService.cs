#nullable enable

using Microsoft.JSInterop;
using ZeroDawn.Shared.Services;

namespace ZeroDawn.Web.Client.Services;

public class BrowserPreferencesService : IPreferencesService
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserPreferencesService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch
        {
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch
        {
        }
    }
}
