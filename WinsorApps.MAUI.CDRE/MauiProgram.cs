using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Global.Services;
using WinsorApps.MAUI.CDRE.Pages;
using WinsorApps.MAUI.CDRE.ViewModels;

namespace WinsorApps.MAUI.CDRE
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
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("fa-regular-400.ttf", "FontAwesomeRegular");
                    fonts.AddFont("fa-solid-900.ttf", "FontAwesomeSolid");
                    fonts.AddFont("fa-brands-400.ttf", "FontAwesomeBrands");
                    fonts.AddFont("CrimsonText-Semibold.ttf", "Serif");
                    fonts.AddFont("NotoSans-Regular.ttf", "SansSerif");
                })
                .UseMauiCommunityToolkitCore()
                .AddGlobalServices()
                .UseAcrylicView();

            builder.Services.AddSingleton<CycleDayRecurringEventService>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<EventListViewModel>();
            builder.Services.AddSingleton<EventsListPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            ServiceHelper.Initialize(app.Services);

            var api = ServiceHelper.GetService<ApiService>();

            var logging = ServiceHelper.GetService<LocalLoggingService>();
            api.Initialize(logging.LogError)
                .SafeFireAndForget(e => e.LogException());

            return app;
        }
    }
}
