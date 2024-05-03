using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class SplashPageViewModel : ObservableObject
{
    [ObservableProperty] string title;

    [ObservableProperty] string subTitle;

    [ObservableProperty] ImmutableArray<string> messages;

    [ObservableProperty] bool isCaptive;

    [ObservableProperty] TimeSpan timeout;

    public event EventHandler? OnClose;

    public SplashPageViewModel(string title, ImmutableArray<string>? startingMessages = null, TimeSpan? timeout = null)
    {
        this.title = title;
        subTitle = "";
        messages = startingMessages ?? [];
        isCaptive = true;
        this.timeout = timeout ?? TimeSpan.MaxValue;


    }

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