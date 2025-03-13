namespace WinsorApps.Services.Global.Services;

public interface IAsyncInitService
{
    public string CacheFileName { get; }

    public Task SaveCache();

    public void ClearCache();

    /// <summary>
    /// Attempt to Load Cache from file.  returns success status.
    /// </summary>
    /// <returns></returns>
    public bool LoadCache();

    public Task Initialize(ErrorAction onError);

    public Task WaitForInit(ErrorAction onError);

    public Task Refresh(ErrorAction onError);

    public bool Started { get; } 
    
    public bool Ready { get; }
    
    public double Progress { get; }
}