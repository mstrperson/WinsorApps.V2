using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class SplashPageViewModel(string title, List<string>? startingMessages = null, TimeSpan? timeout = null) : ObservableObject
{
    [ObservableProperty] string title = title;

    [ObservableProperty] string subTitle = "";

    [ObservableProperty] List<string> messages = startingMessages ?? [];

    [ObservableProperty] bool isCaptive = true;

    [ObservableProperty] TimeSpan timeout = timeout ?? TimeSpan.MaxValue;

    public event EventHandler? OnClose;

    private void TimeoutFireAndForget()
    {
        var task = Task.Delay(Timeout);
        task.WhenCompleted(Close);
        task.SafeFireAndForget(e => e.LogException(ServiceHelper.GetService<LocalLoggingService>()));
    }

    [RelayCommand]
    public void Close()
    {
        OnClose?.Invoke(this, EventArgs.Empty);
    }

}