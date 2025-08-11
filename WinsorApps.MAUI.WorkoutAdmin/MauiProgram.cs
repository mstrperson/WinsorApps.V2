global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;

using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.MAUI.WorkoutAdmin.Pages;
using WinsorApps.MAUI.WorkoutAdmin.ViewModels;
using WinsorApps.Services.Athletics.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.WorkoutAdmin
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

            builder.Services.AddSingleton<OpenWorkouts>();
            builder.Services.AddSingleton<ReportBuilderViewModel>();
            builder.Services.AddSingleton<MainPage>();

            var app = builder.Build();

            ServiceHelper.Initialize(app.Services);

            var logging = ServiceHelper.GetService<LocalLoggingService>();

            var credManager = ServiceHelper.GetService<ISavedCredential>() ?? SavedCredential.Default;

            if (!credManager.SavedCredExists)
            {
                credManager.Save("athletics.signin@winsor.edu", "#&#FYQ055zbk");
            }

            var api = ServiceHelper.GetService<ApiService>();
            api.Initialize(err => logging.LogMessage(LocalLoggingService.LogLevel.Error,
                err.type, err.error)).SafeFireAndForget(e => e.LogException(logging));


            var workoutService = ServiceHelper.GetService<WorkoutService>();
            workoutService.Initialize(logging.LogError).SafeFireAndForget(e => e.LogException(logging));

            var registrarService = ServiceHelper.GetService<RegistrarService>();

            api.OnLoginSuccess += (_, _) =>
            {
                registrarService.Initialize(logging.LogError)
                    .SafeFireAndForget(e => e.LogException());
            };
            var helpPage = ServiceHelper.GetService<HelpPageViewModel>();


            helpPage.Services =
            [
                new(registrarService, "Registrar Service"),
                new(workoutService, "Workout Service")
            ];



            logging.LogMessage(LocalLoggingService.LogLevel.Information, "App Built Successfully!");
            return app;
        }
    }
}
