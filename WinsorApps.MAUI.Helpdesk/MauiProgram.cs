global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;

using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.Helpdesk.Pages;
using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
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

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<QuickCheckinViewModel>();
        builder.Services.AddSingleton<QuickCheckoutViewModel>();
        builder.Services.AddSingleton<QuickCheckout>();
        builder.Services.AddSingleton<QuickCheckin>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        ServiceHelper.Initialize(app.Services);

        var logging = ServiceHelper.GetService<LocalLoggingService>();
        ServiceHelper.GetService<ApiService>().Initialize(err => logging.LogMessage(LocalLoggingService.LogLevel.Error,
            err.type, err.error)).SafeFireAndForget(e => e.LogException(logging));
        
        return app;
    }

    
}