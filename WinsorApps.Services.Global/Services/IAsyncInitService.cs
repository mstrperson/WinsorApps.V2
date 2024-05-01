namespace WinsorApps.Services.Global.Services;

public interface IAsyncInitService
{
    public Task Initialize(ErrorAction onError);

    public bool Started { get; } 
    
    public bool Ready { get; }
    
    public double Progress { get; }
}