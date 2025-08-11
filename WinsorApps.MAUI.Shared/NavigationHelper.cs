using CommunityToolkit.Mvvm.ComponentModel;

namespace WinsorApps.MAUI.Shared;

public static class NavigationHelper
{
    /// <summary>
    /// Encapsulates a call to Navigation.PushAsync from anywhere
    /// </summary>
    /// <param name="page">the page to push</param>
    /// <param name="windowNumber">Window number that is part of the call chain</param>
    /// <returns>if any part of the call chain is null, this returns a completed task.</returns>
    public static async Task TryPushAsync(this ContentPage page, int windowNumber = 0) => 
        await (
            Application.Current?.Windows[windowNumber].Page?.Navigation.PushAsync(page, true)
            ?? Task.CompletedTask
        );

    /// <summary>
    /// Encapsulates a call to Navigation.PushAsync in a synchronous context
    /// .. This does /not/ wait for completion.
    /// </summary>
    /// <param name="page">the page to push</param>
    /// <param name="windowNumber">Window number that is part of the call chain</param>
    public static void TryPush(this ContentPage page, int windowNumber = 0) =>
            Application.Current?.Windows[windowNumber].Page?.Navigation.PushAsync(page, true);

    /// <summary>
    /// Encapsulates a call to Navigation.PopAsync to call within any ViewModel type 
    /// by invoking this.TryPopAsync()
    /// </summary>
    /// <param name="_">call from within any ViewModel</param>
    /// <param name="windowNumber">Window number that is part of the call chain</param>
    /// <returns>if any part of the call chain is null, this returns a completed task.</returns>
    public static async Task TryPopAsync(this ObservableObject _, int windowNumber = 0) =>
        await (
            Application.Current?.Windows[windowNumber].Page?.Navigation.PopAsync(true)
            ?? Task.CompletedTask
        );

    /// <summary>
    /// Encapsulates a call to Navigation.PopAsync to call within any ViewModel type 
    /// by invoking this.TryPopAsync()
    /// </summary>
    /// <param name="_">call from within any ViewModel</param>
    /// <param name="windowNumber">Window number that is part of the call chain</param>
    public static void TryPop(this ObservableObject _, int windowNumber = 0) =>
            Application.Current?.Windows[windowNumber].Page?.Navigation.PopAsync(true);

}
