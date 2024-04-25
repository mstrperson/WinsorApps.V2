using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared;

public static class Extensions
{
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

    public static T GetResource<T>(this object _, string dictionary, string key) => (T)Resources[dictionary][key];

    public static Color GetColorResource(this object _, string colorKey) => (Color)Resources["Colors"][colorKey];

    public static MauiAppBuilder AddGlobalServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<RegistrarService>();
        builder.Services.AddSingleton<AppService>();
        builder.Services.AddSingleton<LocalLoggingService>();

        return builder;
    }
}

/// <summary>
/// Helper class for accessing Services from Dependency Injection.
/// </summary>
public static class ServiceHelper
{
    public static IServiceProvider? Services { get; private set; } = null!;

    public static void Initialize(IServiceProvider sp) => Services = sp;

    public static T? GetService<T>() => Services is null ? default : Services.GetService<T>();
}