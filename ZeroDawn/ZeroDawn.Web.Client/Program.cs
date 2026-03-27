using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Globalization;
using ZeroDawn.Shared.Core;
using ZeroDawn.Shared.Services;
using ZeroDawn.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");
var defaultCulture = new CultureInfo("ar");

CultureInfo.CurrentCulture = defaultCulture;
CultureInfo.CurrentUICulture = defaultCulture;
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

// Add device-specific services used by the ZeroDawn.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddAuthorizationCore();
builder.Services.AddLocalization(options => options.ResourcesPath = "Localization/Resources");
builder.Services.AddSingleton<IConnectivityService, WebConnectivityService>();
builder.Services.AddScoped<IPreferencesService, BrowserPreferencesService>();
builder.Services.AddScoped<ITokenStorageService, BrowserTokenStorage>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IAuthService, DefaultAuthService>();
builder.Services.AddScoped<WebAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<WebAuthStateProvider>());
builder.Services.AddScoped<AuthHttpHandler>();
builder.Services.AddHttpClient<IAuthApiClient, DefaultAuthApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthHttpHandler>();
builder.Services.AddHttpClient("ApiAuthenticated", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthHttpHandler>();
builder.Services.AddHttpClient("ApiNoAuth", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

await builder.Build().RunAsync();
