using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Example;

public partial class MainPage : ContentPage
{
    private readonly LocalLoggingService _logging;
    private readonly ApiService _api;

    /// <summary>
    /// Dependency Injection!
    /// </summary>
    /// <param name="logging"></param>
    /// <param name="api"></param>
    public MainPage(LocalLoggingService logging, ApiService api)
    {
        _logging = logging;
        _api = api;
        InitializeComponent();
    }

    /// <summary>
    /// WHEN the Login Button on the MainPage is clicked...
    /// create a new LoginPage,
    /// Connect up the OnLogin and OnError events
    /// then Push the new page to the front of the Navigation stack.
    /// (This means display the LoginPage you just created~
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_OnClicked(object? sender, EventArgs e)
    {
        LoginPage page = new(_logging);
        page.ViewModel.OnLogin += ViewModelOnOnLogin;
        page.ViewModel.OnError += ViewModelOnOnError;
        Navigation.PushAsync(page);
    }

    /// <summary>
    /// WHEN an error occurs...
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ViewModelOnOnError(object? sender, ErrorRecord e)
    {
        // TODO:  We should probably do something here....
    }

    /// <summary>
    /// WHEN the user Successfully Logs in...
    /// Create a new UserInfoPage using the logged in UserInfo...
    /// And present that UserInfoPage on top of the Navigation Stack.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ViewModelOnOnLogin(object? sender, EventArgs e)
    {
        var vm = new UserViewModel(_api.UserInfo!.Value);
        UserInfoPage page = new() {BindingContext = vm};
        Navigation.PushAsync(page);
    }
}