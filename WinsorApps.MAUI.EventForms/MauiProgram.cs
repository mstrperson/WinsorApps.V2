﻿using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.EventForms.Pages;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.EventForms
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
                    fonts.AddFont("CrimsonText-Semibold.ttf", "Serif");
                    fonts.AddFont("NotoSans-Regular.ttf", "SansSerif");
                })
                .UseMauiCommunityToolkitCore()
                .AddEventFormsServices()
                .AddGlobalServices();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<MyEventsList>();
            builder.Services.AddTransient<EventListViewModel>();

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
                new(ServiceHelper.GetService<EventFormsService>(), "Event Forms Service"),
                new(ServiceHelper.GetService<LocationService>(), "Location Service"),
                new(ServiceHelper.GetService<BudgetCodeService>(), "Budget Code Service"),
                new(ServiceHelper.GetService<ReadonlyCalendarService>(), "Calendar Service"),
                new(ServiceHelper.GetService<TheaterService>(), "Theater Service"),
                new(ServiceHelper.GetService<ContactService>(), "Contact Service"),
                new(ServiceHelper.GetService<CateringMenuService>(), "Catering Menu Service")
            ];

            return app;
        }
    }
}