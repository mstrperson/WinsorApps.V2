
global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Bookstore;
using WinsorApps.MAUI.Shared.Pages;

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
                    fonts.AddFont("CrimsonText-Semibold.ttf", "Serif");
                    fonts.AddFont("NotoSans-Regular.ttf", "SansSerif");
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
            .UseMauiCommunityToolkitCore()
            .AddGlobalServices()
            .AddBookstoreServices();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<LoginPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
