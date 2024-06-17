global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;

using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.Helpdesk.Pages;
using WinsorApps.MAUI.Helpdesk.Pages.Devices;
using WinsorApps.MAUI.Helpdesk.ViewModels;
using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
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
                fonts.AddFont("CrimsonText-Semibold.ttf", "Serif");
                fonts.AddFont("NotoSans-Regular.ttf", "SansSerif");
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
        builder.Services.AddSingleton<CheckoutSearchViewModel>();
        builder.Services.AddSingleton<QuickCheckoutViewModel>();
        builder.Services.AddSingleton<CheqroomQuickTasksViewModel>();
        builder.Services.AddSingleton<CheqroomQuickTaskPage>();
        builder.Services.AddSingleton<CheckoutSearchPage>();
        builder.Services.AddSingleton<DeviceSearchPage>();
        builder.Services.AddTransient<DeviceDetailsPage>();
        builder.Services.AddSingleton<HudViewModel>();
        builder.Services.AddSingleton<HUD>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        ServiceHelper.Initialize(app.Services); 

        var logging = ServiceHelper.GetService<LocalLoggingService>();
        ServiceHelper.GetService<ApiService>().Initialize(err => logging.LogMessage(LocalLoggingService.LogLevel.Error,
            err.type, err.error)).SafeFireAndForget(e => e.LogException(logging));

        var helpPage = ServiceHelper.GetService<HelpPageViewModel>();
        helpPage.Services =
        [
            new(ServiceHelper.GetService<DeviceService>(), "Device Service"),
            new(ServiceHelper.GetService<JamfService>(), "Jamf Service"),
            new(ServiceHelper.GetService<CheqroomService>(), "Cheqroom Service"),
            new(ServiceHelper.GetService<ServiceCaseService>(), "Service Cases")
        ];
        
        return app;
    }

    
}