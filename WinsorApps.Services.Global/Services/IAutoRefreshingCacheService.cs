namespace WinsorApps.Services.Global.Services;

public interface IAutoRefreshingCacheService : ICacheService
{
    public TimeSpan RefreshInterval { get; }

    public bool Refreshing { get; }

    public Task RefreshInBackground(CancellationToken token, ErrorAction onError);
}

public interface ICacheService
{
    public event EventHandler? OnCacheRefreshed;
}
