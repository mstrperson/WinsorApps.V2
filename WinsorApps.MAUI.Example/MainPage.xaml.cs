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

    private void Button_OnClicked(object? sender, EventArgs e)
    {
        LoginPage page = new(_logging);
        page.ViewModel.OnLogin += ViewModelOnOnLogin;
        page.ViewModel.OnError += ViewModelOnOnError;
        Navigation.PushAsync(page);
    }

    private void ViewModelOnOnError(object? sender, ErrorRecord e)
    {
        
    }

    private void ViewModelOnOnLogin(object? sender, EventArgs e)
    {
        var vm = new UserViewModel(_api.UserInfo!.Value);
        vm.SectionSelected += VmOnSectionSelected;
        UserInfoPage page = new() {BindingContext = vm};
        Navigation.PushAsync(page);
    }

    private void VmOnSectionSelected(object? sender, SectionRecord e)
    {
        
    }
}