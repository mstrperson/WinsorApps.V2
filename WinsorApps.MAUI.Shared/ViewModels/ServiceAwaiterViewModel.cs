using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class ServiceAwaiterViewModel : ObservableObject
{
    private readonly IAsyncInitService _service;

    [ObservableProperty] private bool ready;
    [ObservableProperty] private string serviceName;
    [ObservableProperty] private double progress;
    [ObservableProperty] private bool started;

    public event EventHandler? OnCompletion;
    public event EventHandler<ErrorRecord>? OnError;

    public ServiceAwaiterViewModel(IAsyncInitService service, string name)
    {
        _service = service;
        serviceName = name;
        ready = false;
        BackgroundAwaiter().SafeFireAndForget();
    }

    [RelayCommand]
    public void Initialize()
    {
        if (_service.Started) return;
        
        _service.Initialize(err => 
                OnError?.Invoke(this, err))
            .SafeFireAndForget(e => 
                OnError?.Invoke(this, new("Intialization Exception", e.Message)));
    }
    private async Task BackgroundAwaiter()
    {
        while (!_service.Ready)
        {
            await Task.Delay(250);
            Progress = _service.Progress;
            Started = _service.Started;
        }
        
        Ready = true;
        OnCompletion?.Invoke(this, EventArgs.Empty);
    }
}