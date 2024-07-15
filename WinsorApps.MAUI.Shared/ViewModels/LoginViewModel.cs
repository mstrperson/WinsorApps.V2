using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class LoginViewModel : 
    ObservableObject,
    IBusyViewModel
{
    /***************************************
     * Ok, so ObservableProperty ...
     * these are the backing fields for the
     * capital letter versions that you use
     * everywhere else.
     *
     * This is secretly updating the Views
     * that bind to this data whenever the
     * variables are changed.
     ***************************************/
    
    [ObservableProperty] private string email;
    [ObservableProperty] private string password;
    [ObservableProperty] private bool isLoggedIn = false;
    [ObservableProperty] private bool showPasswordField = true;
    [ObservableProperty] private string statusMessage;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "Working...";

    private readonly ApiService _api;

    /// <summary>
    /// When the user successfully logs in ....
    /// </summary>
    public event EventHandler? OnLogin;
    
    /// <summary>
    /// When the user logs out....
    /// </summary>
    public event EventHandler? OnLogout;
    
    /// <summary>
    /// When an error occurs ....
    /// </summary>
    public event EventHandler<ErrorRecord>? OnError;
    
    /// <summary>
    /// When the user's password has been reset ....
    /// </summary>
    public event EventHandler<string>? OnForgotPassword;

    public LoginViewModel()
    {
        // Inject this service from the Service Helper!
        _api = ServiceHelper.GetService<ApiService>();
        busy = _api.AutoLoginInProgress;
        _api.OnLoginSuccess += _api_OnLoginSuccess;
        email = "";
        password = "";
        statusMessage = _api.Ready ? "Login Successful" : busy ? "Waiting for Auto Login" : "Please Log In";
        isLoggedIn = _api.Ready;
        
    }

    private async Task WaitForAutoLogin()
    {
        while (_api.AutoLoginInProgress)
            await Task.Delay(100);
        Busy = false;
        StatusMessage = _api.Ready ? "Login Successful" : "Please Login";
    }

    private void _api_OnLoginSuccess(object? sender, EventArgs e)
    {
        OnLogin?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// A Relay Command is a method that can be linked to a view and `Bound`
    /// to the Command parameter of some UI elements (Like Buttons)
    ///
    /// This Command Relays the users intent to Login to the ApiService
    /// and provides the input that the user has Typed into the Email and
    /// Password properties of this ViewModel.
    /// </summary>
    [RelayCommand]
    public async Task Login()
    {
        await _api.Login(Email.ToLowerInvariant(), Password, 
            err =>
            {
                StatusMessage = err.error;
                OnError?.Invoke(this, err);
            });
        IsLoggedIn = _api.Ready;
    }

    /// <summary>
    /// Provides the View with access to the ApiService Logout method.
    /// Once this is completed, it will invoke the OnLogout event
    /// So the View can update `when the user logs out ...`
    /// </summary>
    [RelayCommand]
    public void Logout()
    {
        _api.Logout();
        OnLogout?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The View can access the ForgotPassword method in the ApiService.
    /// Two possible outcomes:
    /// Success -> `when password is reset...`
    /// Error -> `when an error occurs ...`
    /// </summary>
    [RelayCommand]
    public async Task ForgotPassword()
    {
        await _api.ForgotPassword(Email, "", 
            str => OnForgotPassword?.Invoke(this, "Email Sent"),
            err => OnError?.Invoke(this, err));
    }

}