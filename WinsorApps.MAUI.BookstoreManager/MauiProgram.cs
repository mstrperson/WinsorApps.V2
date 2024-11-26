
global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;
using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.BookstoreManager.Pages;
using WinsorApps.MAUI.BookstoreManager.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Bookstore;
using WinsorApps.MAUI.Shared.Bookstore.Pages;
using WinsorApps.MAUI.Shared.Bookstore.ViewModels;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.BookstoreManager
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
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<BookSearchViewModel>();
            builder.Services.AddSingleton<BookSearchPage>();
            builder.Services.AddSingleton<StudentPageViewModel>();
            builder.Services.AddSingleton<StudentCheckout>(); 
            builder.Services.AddSingleton<ReportsPageViewModel>();
            builder.Services.AddSingleton<ReportPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            ServiceHelper.Initialize(app.Services);

            var logging = ServiceHelper.GetService<LocalLoggingService>();
            ServiceHelper.GetService<ApiService>().Initialize(err => logging.LogMessage(LocalLoggingService.LogLevel.Error,
                err.type, err.error)).SafeFireAndForget(e => e.LogException(logging));

            var bsp = ServiceHelper.GetService<BookSearchPage>();
            bsp.ViewModel.BookSelected += (sender, book) =>
            {
                ManagerBookEditor page = new() { BindingContext = book };
                
            };

            return app;
        }
    }
}
