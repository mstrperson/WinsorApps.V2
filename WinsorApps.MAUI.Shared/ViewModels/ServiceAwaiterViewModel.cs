using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class TaskAwaiterViewModel : ObservableObject
{
    private readonly Task _task;

    [ObservableProperty] private bool ready;
    [ObservableProperty] private string taskName;

    public TaskAwaiterViewModel(Task task, string taskName)
    {
        _task = task;
        _task.WhenCompleted(() => { Ready = true; });
        this.taskName = taskName;
    }

    [RelayCommand]
    public void Start()
    {
        try
        {
            _task.Start();
        }
        catch { }

    }
}

public partial class ServiceAwaiterViewModel : ObservableObject
{
    private readonly IAsyncInitService _service;

    [ObservableProperty] private bool ready;
    [ObservableProperty] private string serviceName;
    [ObservableProperty] private double progress;
    [ObservableProperty] private bool started;
    [ObservableProperty] private bool runInBackground;

    public event EventHandler? OnCompletion;
    public event EventHandler<ErrorRecord>? OnError;

    public ServiceAwaiterViewModel(IAsyncInitService service, string name, bool runInBackground = false)
    {
        _service = service;
        serviceName = name;
        ready = false;
        RunInBackground = runInBackground;  
        BackgroundAwaiter().SafeFireAndForget(e => e.LogException());
    }

    [RelayCommand]
    public void ClearCache()
    {
        _service.ClearCache();
    }

    [RelayCommand]
    public async Task Initialize()
    {
        if (_service.Started) return;
        
        await _service
            .Initialize(err => 
                OnError?.Invoke(this, err));
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
        Progress = _service.Progress;
        Started = _service.Started;
        OnCompletion?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task Refresh()
    {
        await _service.Refresh(OnError.DefaultBehavior(this));
    }
}