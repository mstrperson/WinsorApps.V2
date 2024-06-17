﻿using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui.Core;
using WinsorApps.Services.Global;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Global.Services;
using AsyncAwaitBestPractices;

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
                })
                .UseMauiCommunityToolkitCore()
                .AddGlobalServices();

            builder.Services.AddSingleton<CycleDayRecurringEventService>();

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
