using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk;

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
            .UseMauiCommunityToolkitCore()
            .AddGlobalServices();

        builder.Services.AddSingleton<JamfService>();
        builder.Services.AddSingleton<DeviceService>();
        builder.Services.AddSingleton<CheqroomService>();
        builder.Services.AddSingleton<ServiceCaseService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        app.InitializeGlobalServices();
        var api = ServiceHelper.GetService<ApiService>()!;

        api.OnLoginSuccess += (_, _) =>
        {
            var logging = ServiceHelper.GetService<LocalLoggingService>()!;
            var jamf = ServiceHelper.GetService<JamfService>()!;
            jamf.Initialize(OnError)
                .SafeFireAndForget(e => e.LogException(logging));

            var serviceCases = ServiceHelper.GetService<ServiceCaseService>()!;
            serviceCases.Initialize(OnError)
                .SafeFireAndForget(e => e.LogException(logging));

            var deviceService = ServiceHelper.GetService<DeviceService>()!;
            deviceService.Initialize(OnError)
                .SafeFireAndForget(e => e.LogException(logging));
        };

        return app;
    }

    
    private static void OnError(ErrorRecord err)
    {
        var logging = ServiceHelper.GetService<LocalLoggingService>()!;
        logging.LogMessage(LocalLoggingService.LogLevel.Error, err.type, err.error);
    }
}