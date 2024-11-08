global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;

using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;
using WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar
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
            builder.Services.AddSingleton<ReadonlyCalendarService>();
            builder.Services.AddSingleton<TeacherAssessmentService>();
            builder.Services.AddSingleton<AssessmentCalendarRestrictedService>();

            builder.Services.AddSingleton<MyAssessmentsCollectionViewModel>();
            builder.Services.AddSingleton<MyAssessmentsPageViewModel>();
            builder.Services.AddSingleton<MyAssessmentsPage>();

            builder.Services.AddSingleton<AllMyStudentsViewModel>();
            builder.Services.AddSingleton<StudentPageViewModel>();
            builder.Services.AddSingleton<StudentPage>();

            builder.Services.AddSingleton<LateWorkPageViewModel>();
            builder.Services.AddSingleton<LateWorkPage>();

            builder.Services.AddSingleton<MonthlyCalendarViewModel>();
            builder.Services.AddSingleton<MonthlyCalendar>();


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
                new(ServiceHelper.GetService<ReadonlyCalendarService>(), "Read Only Calendar Service"),
                new(ServiceHelper.GetService<TeacherAssessmentService>(), "Teacher Calendar Service"),
                new(ServiceHelper.GetService<RegistrarService>(), "Registrar Service"),
            ];

            logging.LogMessage(LocalLoggingService.LogLevel.Information, "App Built Successfully!");
            return app;
        }
    }
}
