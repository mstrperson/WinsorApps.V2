global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;
using AndroidX.SavedState;
using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Athletics;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Athletics.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.WorkoutSignIn
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
                .AddWorkoutServices();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            ServiceHelper.Initialize(app.Services);

            var logging = ServiceHelper.GetService<LocalLoggingService>();

            if(!SavedCredential.SavedCredExists)
            {
                SavedCredential.Save("athletics.signin@winsor.edu", "#&#FYQ055zbk");
            }

            ServiceHelper.GetService<ApiService>().Initialize(err => logging.LogMessage(LocalLoggingService.LogLevel.Error,
                err.type, err.error)).SafeFireAndForget(e => e.LogException(logging));

            var helpPage = ServiceHelper.GetService<HelpPageViewModel>();
            helpPage.Services =
            [
                new(ServiceHelper.GetService<RegistrarService>(), "Registrar Service"),
                new(ServiceHelper.GetService<WorkoutService>(), "Workout Service")
            ];

            logging.LogMessage(LocalLoggingService.LogLevel.Information, "App Built Successfully!");
            return app;
        }
    }
}
