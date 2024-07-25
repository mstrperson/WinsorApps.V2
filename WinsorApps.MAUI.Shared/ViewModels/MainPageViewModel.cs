using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class MainPageViewModel : ObservableObject, IBusyViewModel, IErrorHandling
{
    [ObservableProperty] private List<ServiceAwaiterViewModel> postLoginServices = [];
    [ObservableProperty] private List<TaskAwaiterViewModel> completion = [];
    [ObservableProperty] private SplashPageViewModel splashPageVM;
    [ObservableProperty] private LoginViewModel loginVM;
    [ObservableProperty] private UserViewModel userVM;
    [ObservableProperty] private bool ready;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "Loading Data... Please Wait";

    private string _appId = "";

    public string AppId
    {
        get => _appId;
        set
        {
            _appId = value;
            var apiTask = _api.WaitForInit(OnError.DefaultBehavior(this));
            apiTask.WhenCompleted( () =>
            {
                var allowedTask = _appService.AmIAllowed(_appId, OnError.DefaultBehavior(this));
                allowedTask.WhenCompleted(async () =>
                {
                    if (!allowedTask.Result) // 
                    {
                        OnError?.Invoke(this, new("App Not Authorized", "You aren't authorized to use this particular app."));
                        await Task.Delay(5000);
                        Logout();
                    }
                });
            });
        }
    }
    
    private readonly AppService _appService;
    private readonly ApiService _api;
    public event EventHandler<SplashPageViewModel>? OnSplashPageReady;
    public event EventHandler? OnCompleted;
    public event EventHandler<ErrorRecord>? OnError;

    public MainPageViewModel(List<ServiceAwaiterViewModel> postLoginServices, AppService appService, ApiService api)
    {
        userVM = UserViewModel.Default;
        loginVM = new();
        loginVM.OnLogin += LoginVMOnOnLogin;
        loginVM.OnError += (sender, e) => OnError?.Invoke(sender, e);
        loginVM.OnLogout += LoginVMOnOnLogout;
        loginVM.OnForgotPassword += LoginVMOnOnForgotPassword;
        this.postLoginServices = postLoginServices;
        _appService = appService;
        _api = api;
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
        Busy = true;
        OnSplashPageReady?.Invoke(this, SplashPageVM);
        var api = ServiceHelper.GetService<ApiService>()!;
        UserVM = UserViewModel.Get(api.UserInfo!.Value);
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
        UserVM = UserViewModel.Get(api.UserInfo!.Value);

        foreach (var task in Completion)
            task.Start();

        while (Completion.Any(task => !task.Ready))
            await Task.Delay(250);

        OnCompleted?.Invoke(this, EventArgs.Empty);
        Ready = true;
        Busy = false;
    }

    [RelayCommand]
    public void Logout()
    {
        Busy = true;
        BusyMessage = "Logging out.";
        _api.Logout();

        Thread.Sleep(1000);
        Application.Current?.Quit();
    }

}