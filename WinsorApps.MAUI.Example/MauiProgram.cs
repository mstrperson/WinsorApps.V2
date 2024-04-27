using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Example;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseMauiCommunityToolkitCore();

        // This is Dependency Injection!!
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<RegistrarService>();
        builder.Services.AddSingleton<AppService>();
        builder.Services.AddSingleton<LocalLoggingService>();

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<UserInfoPage>();
        builder.Services.AddSingleton<LoginPage>();
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Initialize the ServiceHelper...
        ServiceHelper.Initialize(app.Services);

        var api = ServiceHelper.GetService<ApiService>()!;
        
        api.OnLoginSuccess += ApiOnOnLoginSuccess;
        
        
        return app;
    }

    private static void ApiOnOnLoginSuccess(object? sender, EventArgs e)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>()!;
        var logging = ServiceHelper.GetService<LocalLoggingService>()!;
        registrar.Initialize(err =>
            logging.LogMessage(LocalLoggingService.LogLevel.Error,
                err.type, err.error))
            .SafeFireAndForget(e => e.LogException(logging));
    }
}