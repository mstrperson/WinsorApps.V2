global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;
using WinsorApps.MAUI.Shared.Athletics.ViewModels;
using WinsorApps.Services.Athletics.Services;

namespace WinsorApps.MAUI.Shared.Athletics;

// All the code in this file is included in all platforms.
public static class Extensions
{
    public static MauiAppBuilder AddWorkoutServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<WorkoutService>();
        builder.Services.AddSingleton<NewWorkoutViewModel>();
        builder.Services.AddSingleton<SignInPageViewModel>();

        return builder;
    }
}
