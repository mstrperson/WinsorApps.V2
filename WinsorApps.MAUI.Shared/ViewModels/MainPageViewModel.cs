using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty] private List<ServiceAwaiterViewModel> postLoginServices;
    [ObservableProperty] private SplashPageViewModel splashPageVM;
    [ObservableProperty] private LoginViewModel loginVM;
    [ObservableProperty] private UserViewModel userVM;
    [ObservableProperty] private bool ready;

    public event EventHandler<SplashPageViewModel>? OnSplashPageReady;
    public event EventHandler? OnCompleted;
    public event EventHandler<ErrorRecord>? OnError;

    public MainPageViewModel(List<ServiceAwaiterViewModel> postLoginServices)
    {
        userVM = UserViewModel.Empty;
        loginVM = new();
        loginVM.OnLogin += LoginVMOnOnLogin;
        loginVM.OnError += (sender, e) => OnError?.Invoke(sender, e);
        loginVM.OnLogout += LoginVMOnOnLogout;
        loginVM.OnForgotPassword += LoginVMOnOnForgotPassword;
        this.postLoginServices = postLoginServices;
        foreach (var serv in PostLoginServices)
        {
            serv.OnCompletion += (_, _) => SplashPageVM.Messages =
                [..SplashPageVM.Messages.Except([$"Waiting for {serv.ServiceName}"])];
            serv.OnError += (sender, e) => OnError?.Invoke(sender, e);
        }
        splashPageVM = new("Getting your Data",
            postLoginServices.Select(serv => 
                $"Waiting for {serv.ServiceName}").ToImmutableArray());
        
        BackgroundAwaiter().SafeFireAndForget(e => OnError?.Invoke(this, new("Initialization Error", e.Message)));
    }

    private void LoginVMOnOnForgotPassword(object? sender, string e)
    {
    }

    private void LoginVMOnOnLogout(object? sender, EventArgs e)
    {
    }

    private void LoginVMOnOnLogin(object? sender, EventArgs e)
    {
        OnSplashPageReady?.Invoke(this, SplashPageVM);
        var api = ServiceHelper.GetService<ApiService>()!;
        UserVM = new(api.UserInfo!.Value);
        foreach (var serv in PostLoginServices.Where(serv => !serv.Started))
            serv.Initialize();
    }

    private async Task BackgroundAwaiter()
    {
        while (PostLoginServices.Any(serv => !serv.Ready))
        {
            await Task.Delay(250);
        }

        var api = ServiceHelper.GetService<ApiService>()!;
        UserVM = new(api.UserInfo!.Value);
        OnCompleted?.Invoke(this, EventArgs.Empty);
        Ready = true;
    }
}