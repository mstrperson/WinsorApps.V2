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

    public static async Task PushErrorPage(this ContentPage parent, ErrorRecord err, Action? onConfirmAction = null)
    {
        if (err.type.Contains("Unauthorized") 
            || err.error.Contains("find student", StringComparison.InvariantCultureIgnoreCase)
            || err.type.Contains("find student", StringComparison.InvariantCultureIgnoreCase))
            return;

        ServiceHelper
            .GetService<LocalLoggingService>()
            .LogMessage(LocalLoggingService.LogLevel.Error,
                        err.type, err.error);

        if (onConfirmAction is null)
        {
            await (Application.Current?.Windows[0].Page?.DisplayAlert(
                err.type,
                err.error, 
                "Ok") 
                ?? Task.CompletedTask);
            return;
        }

        var confirm = await (Application.Current?.Windows[0].Page?.DisplayAlert(
            err.type,
            err.error,
            "Ok",
            "Cancel") 
            ?? Task.FromResult(false));

        if(confirm)
            onConfirmAction.Invoke();

        /*
        SplashPageViewModel spvm = new(err.type, [err.error], TimeSpan.FromSeconds(30)) { IsCaptive = false };
        spvm.OnClose += (_, _) =>
        {
            onConfirmAction?.Invoke();
            parent.Navigation.PopAsync();
        };

        SplashPage page = new() { BindingContext = spvm };
        parent.Navigation.PushAsync(page);
        */
    }

    public static ErrorAction DefaultOnErrorAction(this ContentPage parent, Action? onConfirmAction = null) => 
        err => parent.PushErrorPage(err, onConfirmAction).SafeFireAndForget(LogException);

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
                _resources = [];
                foreach (var dictionary in Application.Current!.Resources.MergedDictionaries)
                {
                    var key = dictionary.Source.OriginalString.Split(';').First().Split('/').Last().Split('.').First(); // Alternatively If you are good in Regex you can use that as well
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
        var iSavedCredential = ServiceHelper.GetService<ISavedCredential>();

        if (iSavedCredential is CredentialManager manager)
        {
            var task = manager.CheckSavedCredentials();
            task.Wait();
        }

        var logging = ServiceHelper.GetService<LocalLoggingService>();

        var api = ServiceHelper.GetService<ApiService>();
        
        api.Initialize(logging.LogError).SafeFireAndForget(e => e.LogException());
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