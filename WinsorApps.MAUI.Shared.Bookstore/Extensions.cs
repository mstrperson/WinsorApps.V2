global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;
global using WinsorApps.Services.Global;

using AsyncAwaitBestPractices;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Services;
using WinsorApps.MAUI.Shared.Bookstore.Pages;
using WinsorApps.MAUI.Shared.Bookstore.ViewModels;

namespace WinsorApps.MAUI.Shared.Bookstore;

public static partial class Extensions
{
    public static MauiAppBuilder AddBookstoreServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<BookService>();
        builder.Services.AddSingleton<TeacherBookstoreService>();
        builder.Services.AddSingleton<BookstoreManagerService>();
        builder.Services.AddSingleton<StudentBookstoreService>();
        builder.Services.AddSingleton<BookSearchViewModel>();
        builder.Services.AddSingleton<BookSearchPage>();

        return builder;
    }

    private static void InitializeBookstoreServices(this MauiApp app)
    {
        app.InitializeGlobalServices();
        var logging = ServiceHelper.GetService<LocalLoggingService>();
        var bookService = ServiceHelper.GetService<BookService>();
        bookService
            .Initialize(err => logging.LogMessage(LocalLoggingService.LogLevel.Error,
            err.type, err.error))
            .SafeFireAndForget(e => e.LogException(logging));
        
    }

    public static void InitializeTeacherServices(this MauiApp app)
    {
        app.InitializeBookstoreServices();
        var logging = ServiceHelper.GetService<LocalLoggingService>();
        var service = ServiceHelper.GetService<TeacherBookstoreService>();
        service
            .Initialize(err => logging.LogMessage(LocalLoggingService.LogLevel.Error,
                err.type, err.error))
            .SafeFireAndForget(e => e.LogException(logging));
    }
    public static void InitializeStudentServices(this MauiApp app)
    {
        app.InitializeBookstoreServices();
        var logging = ServiceHelper.GetService<LocalLoggingService>();
        var service = ServiceHelper.GetService<StudentBookstoreService>();
        service
            .Initialize(err => logging.LogMessage(LocalLoggingService.LogLevel.Error,
                err.type, err.error))
            .SafeFireAndForget(e => e.LogException(logging));
    }
    public static void InitializeManagerServices(this MauiApp app)
    {
        app.InitializeBookstoreServices();
        var logging = ServiceHelper.GetService<LocalLoggingService>();
        var service = ServiceHelper.GetService<BookstoreManagerService>();
        service
            .Initialize(err => logging.LogMessage(LocalLoggingService.LogLevel.Error,
                err.type, err.error))
            .SafeFireAndForget(e => e.LogException(logging));
    }
}