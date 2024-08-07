﻿global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui.Core;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Bookstore;
using WinsorApps.Services.Global.Services;
using AsyncAwaitBestPractices;
using WinsorApps.MAUI.StudentBookstore.Pages;
using WinsorApps.MAUI.StudentBookstore.ViewModels;

namespace WinsorApps.MAUI.StudentBookstore
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
                .AddBookstoreServices();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<StudentBookstoreViewModel>();
            builder.Services.AddSingleton<SectionRequiredBooksViewModel>();
            builder.Services.AddSingleton<OptionGroupViewModel>();
            builder.Services.AddSingleton<MyCartViewModel>();
            builder.Services.AddSingleton<RequestedBooksPage>();

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
