using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty] private string email;
    [ObservableProperty] private string password;
    [ObservableProperty] private bool isLoggedIn = false;
    [ObservableProperty] private bool showPasswordField = true;
    [ObservableProperty] private string statusMessage;

    private readonly ApiService _api;

    public event EventHandler? OnLogin;
    public event EventHandler? OnLogout;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<string>? OnForgotPassword;

    public LoginViewModel()
    {
        _api = ServiceHelper.GetService<ApiService>()!;
        email = "";
        password = "";
        statusMessage = _api.Ready ? "Login Successful" : "Please Log In";
        isLoggedIn = _api.Ready;
        if(isLoggedIn)
            OnLogin?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task Login()
    {
        await _api.Login(Email, Password, 
            err =>
            {
                StatusMessage = err.error;
                OnError?.Invoke(this, err);
            });
        IsLoggedIn = _api.Ready;
        if(IsLoggedIn)
            OnLogin?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void Logout()
    {
        _api.Logout();
        OnLogout?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task ForgotPassword()
    {
        await _api.ForgotPassword(Email, "", 
            str => OnForgotPassword?.Invoke(this, str),
            err => OnError?.Invoke(this, err));
    }
}