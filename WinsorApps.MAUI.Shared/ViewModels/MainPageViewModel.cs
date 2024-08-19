using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    [ObservableProperty] private bool updateAvailable;
    [ObservableProperty] private string updateLink;
    
    public string AppId
    {
        get => _appService.AppId;
        set => _appService.AppId = value;
    }
    
    private readonly AppService _appService;
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public event EventHandler<SplashPageViewModel>? OnSplashPageReady;
    public event EventHandler? OnCompleted;
    public event EventHandler<ErrorRecord>? OnError;

    public MainPageViewModel(List<ServiceAwaiterViewModel> postLoginServices, AppService appService, ApiService api, LocalLoggingService logging)
    {
        userVM = UserViewModel.Empty;
        loginVM = new();
        loginVM.OnLogin += LoginVMOnOnLogin;
        loginVM.OnError += (sender, e) => OnError?.Invoke(sender, e);
        loginVM.OnLogout += LoginVMOnOnLogout;
        loginVM.OnForgotPassword += LoginVMOnOnForgotPassword;
        this.postLoginServices = postLoginServices;
        _appService = appService;
        _api = api;
        _logging = logging;
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
        if (!_appService.Allowed)
        {
            Busy = true;
            BusyMessage = $"{UserVM.DisplayName} is not able to use this app.  You will now be logged out.";
            await Task.Delay(5000);
            Logout();
        }

        UpdateAvailable = _appService.UpdateAvailable;
        UpdateLink = _appService.GetBrowserLinkForLatestVersion();

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

    [RelayCommand]
    public async Task DownloadLatestVersion()
    {
        Busy = true;
        if (string.IsNullOrEmpty(UpdateLink))
        {
            BusyMessage = "No Update Link Available...";
            await Task.Delay(2000);
            Busy = false;
            return;
        }

        BusyMessage = $"Downloading the Latest App Version...";
        var data = await _api.DownloadFile(UpdateLink, onError: OnError.DefaultBehavior(this));
        var type = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => "exe",
            _ => "pkg"
        };

        string fileName = $"{_appService.Group.appName}.{type}";

        using MemoryStream ms = new(data);
        var result = await FileSaver.Default.SaveAsync(_logging.DownloadsDirectory, fileName, ms);
        if(result.IsSuccessful)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "New Version Downloaded!");
            _appService.LastVersionUpdated = DateTime.Now;
            BusyMessage = "The App will now Exit so  you can install the new version!";
            await Task.Delay(5000);

            Application.Current?.Quit();
        }
        else
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"New Version Download returned {result.Exception?.Message ?? "unsuccessful"}");
            BusyMessage = "Download was not saved.";
            await Task.Delay(2000);
            Busy = false;
        }
    }
}