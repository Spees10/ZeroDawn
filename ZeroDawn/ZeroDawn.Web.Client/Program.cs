using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ZeroDawn.Shared.Core;
using ZeroDawn.Shared.Services;
using ZeroDawn.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

// Add device-specific services used by the ZeroDawn.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ITokenStorageService, BrowserTokenStorage>();
builder.Services.AddScoped<WebAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<WebAuthStateProvider>());
builder.Services.AddScoped<AuthHttpHandler>();
builder.Services.AddHttpClient("ApiNoAuth", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

await builder.Build().RunAsync();
