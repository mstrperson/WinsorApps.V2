global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;
using WinsorApps.MAUI.StudentAssessmentCalendar.Pages;

namespace WinsorApps.MAUI.StudentAssessmentCalendar
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
            builder.Services.AddSingleton<CycleDayCollection>();
            builder.Services.AddSingleton<StudentAssessmentService>();
            builder.Services.AddSingleton<MonthlyViewModel>();
            builder.Services.AddSingleton<MonthlyCalendar>();
            builder.Services.AddSingleton<LatePassCollectionViewModel>();
            builder.Services.AddSingleton<MyLatePasses>();

            builder.Services.AddSingleton<WeeklyViewModel>();
            builder.Services.AddSingleton<WeeklyCalendar>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            var app = builder.Build();

            ServiceHelper.Initialize(app.Services);
            app.InitializeGlobalServices();

            return app;
        }
    }
}
