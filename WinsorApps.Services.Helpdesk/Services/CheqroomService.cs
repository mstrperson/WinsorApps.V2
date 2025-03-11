using AsyncAwaitBestPractices;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.Services.Helpdesk.Services;

public class CheqroomService : IAsyncInitService, IAutoRefreshingCacheService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public ImmutableArray<CheqroomCheckoutSearchResult> OpenOrders
    {
        get;
        private set;
    } = [];

    public bool Started { get; private set; }

    public bool Ready { get; private set; }

    public double Progress { get; private set; } = 0;

    public bool Refreshing { get; private set; }

    public TimeSpan RefreshInterval => TimeSpan.FromMinutes(5);

    public string CacheFileName => throw new NotImplementedException();

    public event EventHandler? OnCacheRefreshed;

    public CheqroomService(ApiService api, LocalLoggingService logging)
    {
        _api = api;
        _logging = logging;
    }

    public async Task<CheqroomCheckoutResult> QuickCheckOutItem(string assetTag, string userId, ErrorAction onError)
    {

        var result = await _api.SendAsync<CheqroomCheckoutResult>(HttpMethod.Get, $"api/cheqroom/quick-checkout?assetTag={assetTag}&userId={userId}",
           onError: onError);

        await Refresh(onError);

        return result;
    }

    public async Task<ImmutableArray<CheqroomCheckoutSearchResult>> GetOpenOrders(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<CheqroomCheckoutSearchResult>>(HttpMethod.Get, $"api/cheqroom/list-checkouts",
            onError: onError);

        return result;
    }

    public async Task SendReminder(string orderId, ErrorAction onError)
    {


        await _api.SendAsync(HttpMethod.Get, $"api/cheqroom/checkouts/{orderId}/reminder",
            onError: onError);
    }



    public async Task CheckInItem(string orderId, ErrorAction onError)
    {
        await _api.SendAsync(HttpMethod.Get, $"api/cheqroom/checkouts/{orderId}/checkin",
            onError: onError);
        var ordre = OpenOrders.Where(ord => ord._id == orderId);
        OpenOrders = [..OpenOrders.Except(ordre)];
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public async Task ForceCheckIn(string deviceId, ErrorAction onError)
    {
        bool retry = false;
        TryAgain:
        var order = OpenOrders.FirstOrDefault(order => order.items.Contains(deviceId));
        if(order == default)
        {
            OpenOrders = await GetOpenOrders(onError);
            if (!retry)
            {
                retry = true;
                goto TryAgain;
            }
            else
            {
                onError(new("Checkout Not Found", $"No Open Checkouts found for device {deviceId}"));
                return;
            }
        }
        await CheckInItem(order._id, onError);
    }

    public async Task<string> GetItemStatus(string id, ErrorAction onError)
    {

        var result = await _api.SendAsync<CheqroomItemStatus>(HttpMethod.Get,
            $"api/devices/{id}/cheqroom/status",
            onError: onError);

        return result.status;
    }

    public async Task<CheqroomItem?> GetItem(string id, ErrorAction onError)
    {
        var result = await _api.SendAsync<CheqroomItem?>(HttpMethod.Get,
            $"api/devices/{id}/cheqroom",
            onError: onError);

        return result;
    }

    public async Task Initialize(ErrorAction onError)
    {
        if (Started) 
            return;
        Started = true;
        Progress = 0;
        OpenOrders = await GetOpenOrders(onError);
        Ready = true;
        Progress = 1;

        RefreshInBackground(CancellationToken.None, onError)
            .SafeFireAndForget(e => e.LogException(_logging));
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        if (!Started)
            await Initialize(onError);

        while (!Ready)
            await Task.Delay(250);
    }

    public async Task Refresh(ErrorAction onError)
    {
        Refreshing = true;
        OpenOrders = await GetOpenOrders(onError);
        Refreshing = false;
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RefreshInBackground(CancellationToken token, ErrorAction onError)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(RefreshInterval, token);
            await Refresh(onError);
        }
    }

    public async Task SaveCache()
    {
        throw new NotImplementedException();
    }

    public bool LoadCache()
    {
        throw new NotImplementedException();
    }
}