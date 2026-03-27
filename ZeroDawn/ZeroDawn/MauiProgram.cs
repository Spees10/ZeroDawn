using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using ZeroDawn.Services;
using ZeroDawn.Shared.Services;

#if ANDROID
using ZeroDawn.Platforms.Android;
#endif

namespace ZeroDawn;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var defaultCulture = new CultureInfo("ar");

        CultureInfo.CurrentCulture = defaultCulture;
        CultureInfo.CurrentUICulture = defaultCulture;
        CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

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
        builder.Services.AddLocalization(options => options.ResourcesPath = "Localization/Resources");
        builder.Services.AddSingleton<IConnectivityService, MauiConnectivityService>();
        builder.Services.AddSingleton<IPreferencesService, MauiPreferencesService>();
        builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();
        builder.Services.AddSingleton<ITokenStorageService, SecureStorageTokenService>();
        builder.Services.AddScoped<IToastService, ToastService>();
        builder.Services.AddScoped<IAuthService, DefaultAuthService>();
        builder.Services.AddScoped<AuthenticationStateProvider, MauiAuthStateProvider>();
        builder.Services.AddTransient<MauiAuthHttpHandler>();
#if ANDROID
        builder.Services.AddHttpClient<IAuthApiClient, DefaultAuthApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
        .ConfigurePrimaryHttpMessageHandler(DevHttpsConnectionHelper.GetPlatformMessageHandler)
        .AddHttpMessageHandler<MauiAuthHttpHandler>();
        builder.Services.AddHttpClient("ApiAuthenticated", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
        .ConfigurePrimaryHttpMessageHandler(DevHttpsConnectionHelper.GetPlatformMessageHandler)
        .AddHttpMessageHandler<MauiAuthHttpHandler>();
        builder.Services.AddHttpClient("ApiNoAuth", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
        .ConfigurePrimaryHttpMessageHandler(DevHttpsConnectionHelper.GetPlatformMessageHandler);
#else
        builder.Services.AddHttpClient<IAuthApiClient, DefaultAuthApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
        .AddHttpMessageHandler<MauiAuthHttpHandler>();
        builder.Services.AddHttpClient("ApiAuthenticated", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
        .AddHttpMessageHandler<MauiAuthHttpHandler>();
        builder.Services.AddHttpClient("ApiNoAuth", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        });
#endif

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
