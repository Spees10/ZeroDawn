using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using ZeroDawn.Services;
using ZeroDawn.Shared.Services;

namespace ZeroDawn;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
#if ANDROID
        const string apiBaseUrl = "https://10.0.2.2:7001";
#elif WINDOWS
        const string apiBaseUrl = "https://localhost:7001";
#else
        const string apiBaseUrl = "https://localhost:7001";
#endif

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add device-specific services used by the ZeroDawn.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddSingleton<IConnectivityService, MauiConnectivityService>();
        builder.Services.AddSingleton<ITokenStorageService, SecureStorageTokenService>();
        builder.Services.AddScoped<AuthenticationStateProvider, MauiAuthStateProvider>();
        builder.Services.AddTransient<MauiAuthHttpHandler>();
        builder.Services.AddHttpClient("ApiNoAuth", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
