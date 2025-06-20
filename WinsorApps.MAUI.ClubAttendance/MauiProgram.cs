using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui.Core;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Clubs.Services;
using WinsorApps.Services.Global.Services;
using WinsorApps.MAUI.Shared.ViewModels;

namespace WinsorApps.MAUI.ClubAttendance
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
                .AddGlobalServices();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<ClubService>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            var app = builder.Build();

            ServiceHelper.Initialize(app.Services);

            var logging = ServiceHelper.GetService<LocalLoggingService>();

            app.InitializeGlobalServices();


            var helpPage = ServiceHelper.GetService<HelpPageViewModel>();
            helpPage.Services =
            [
                new(ServiceHelper.GetService<RegistrarService>(), "Registrar Service"),
                new(ServiceHelper.GetService<ClubService>(), "Club Service")
            ];

            logging.LogMessage(LocalLoggingService.LogLevel.Information, "App Built Successfully!");
            return app;
        }
    }
}
