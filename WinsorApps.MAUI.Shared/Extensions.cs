global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;

using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared;

public static class Extensions
{
    public static void LogException(this Exception e)
    {
        var logging = ServiceHelper.GetService<LocalLoggingService>();
        e.LogException(logging);
    }

    /// <summary>
    /// returns an ErrorAction: err => OnError?.Invoke(sender, err)
    /// </summary>
    /// <param name="OnError"></param>
    /// <param name="sender"></param>
    /// <returns></returns>
    public static ErrorAction DefaultBehavior(this EventHandler<ErrorRecord>? OnError, object? sender) => 
        err => OnError?.Invoke(sender, err);

    public static void Splash(this ContentPage parent, Func<SplashPageViewModel> pageGenerator)
    {
        SplashPage page = new() { BindingContext = pageGenerator() };
        parent.Navigation.PushAsync(page);
    }

    public static void PushErrorPage(this ContentPage parent, ErrorRecord err, Action? onConfirmAction = null)
    {
        if (err.type.Contains("Unauthorized"))
            return;

        ServiceHelper
            .GetService<LocalLoggingService>()
            .LogMessage(LocalLoggingService.LogLevel.Error,
                        err.type, err.error);

        SplashPageViewModel spvm = new(err.type, [err.error], TimeSpan.FromSeconds(30)) { IsCaptive = false };
        spvm.OnClose += (_, _) =>
        {
            if (onConfirmAction is not null)
                onConfirmAction();

            parent.Navigation.PopAsync();
        };

        SplashPage page = new() { BindingContext = spvm };
        parent.Navigation.PushAsync(page);
    }

    public static ErrorAction DefaultOnErrorAction(this ContentPage parent, Action? onConfirmAction = null) => 
        err => parent.PushErrorPage(err, onConfirmAction);

    /// <summary>
    /// Pushes a SplashPage with the Error information provided from the Event.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="onConfirmAction"></param>
    /// <returns></returns>
    public static EventHandler<ErrorRecord> DefaultOnErrorHandler(this ContentPage parent, Action? onConfirmAction = null) => 
        (sender, err) => parent.DefaultOnErrorAction(onConfirmAction)(err);

    private static Dictionary<string, ResourceDictionary> _resources = null!;
    public static Dictionary<string, ResourceDictionary> Resources
    {
        get
        {
            if (_resources is null)
            {
                _resources = new Dictionary<string, ResourceDictionary>();
                foreach (var dictionary in Application.Current!.Resources.MergedDictionaries)
                {
                    string key = dictionary.Source.OriginalString.Split(';').First().Split('/').Last().Split('.').First(); // Alternatively If you are good in Regex you can use that as well
                    _resources.Add(key, dictionary);
                }
            }

            return _resources;
        }
    }

    public static MauiAppBuilder AddGlobalServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<ISavedCredential, CredentialManager>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<RegistrarService>();
        builder.Services.AddSingleton<AppService>();
        builder.Services.AddSingleton<LocalLoggingService>();
        builder.Services.AddSingleton<HelpPageViewModel>();
        builder.Services.AddSingleton<HelpPage>();
        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddSingleton<LoginPage>();

        return builder;
    }

    public static void InitializeGlobalServices(this MauiApp app)
    {
        var api = ServiceHelper.GetService<ApiService>()!;
        api.OnLoginSuccess += (_, _) =>
        {
            var registrar = ServiceHelper.GetService<RegistrarService>()!;
            var logging = ServiceHelper.GetService<LocalLoggingService>()!;
            registrar.Initialize(err =>
                    logging.LogMessage(LocalLoggingService.LogLevel.Error,
                        err.type, err.error))
                .SafeFireAndForget(e => e.LogException(logging));

            UserViewModel.Initialize(registrar, err =>
                    logging.LogMessage(LocalLoggingService.LogLevel.Error,
                        err.type, err.error))
                .SafeFireAndForget(e => e.LogException(logging));
        };


    }
}

/// <summary>
/// Helper class for accessing Services from Dependency Injection.
/// </summary>
public static class ServiceHelper
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static void Initialize(IServiceProvider sp) => Services = sp;

    public static T GetService<T>() => Services.GetService<T>()!;
}