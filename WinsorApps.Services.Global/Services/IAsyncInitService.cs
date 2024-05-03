namespace WinsorApps.Services.Global.Services;

public interface IAsyncInitService
{
    public Task Initialize(ErrorAction onError);

    public Task WaitForInit(ErrorAction onError);

    public Task Refresh(ErrorAction onError);

    public bool Started { get; } 
    
    public bool Ready { get; }
    
    public double Progress { get; }
}