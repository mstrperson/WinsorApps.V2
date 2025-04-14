global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;

using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.MAUI.Shared.EventForms;
using AsyncAwaitBestPractices;
using WinsorApps.Services.EventForms.Services.Admin;
using WinsorApps.MAUI.CateringManagement.ViewModels;
using WinsorApps.MAUI.CateringManagement.Pages;

namespace WinsorApps.MAUI.CateringManagement
{
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
                .AddGlobalServices()
                .AddEventFormsServices();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<EventsAdminService>();
            builder.Services.AddSingleton<CateringEventsPageViewModel>();
            builder.Services.AddSingleton<CateringManagementPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            ServiceHelper.Initialize(app.Services);

            var logging = ServiceHelper.GetService<LocalLoggingService>();
            ServiceHelper.GetService<ApiService>()
                .Initialize(logging.LogError)
                .SafeFireAndForget(e => e.LogException(logging));

            var helpPage = ServiceHelper.GetService<HelpPageViewModel>();
            helpPage.Services =
            [
                new ServiceAwaiterViewModel(ServiceHelper.GetService<EventsAdminService>(), "Event Forms")
            ];

            return app;
        }
    }
}
