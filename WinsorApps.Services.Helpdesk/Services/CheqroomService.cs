using System.Text.Json;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.Services.Helpdesk.Services;

public class CheqroomService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public CheqroomService(ApiService api, LocalLoggingService logging)
    {
        _api = api;
        _logging = logging;
    }

    public async Task<CheqroomCheckoutResult> QuickCheckOutItem(string assetTag, string userId, ErrorAction onError)
        {

            var result = await _api.SendAsync<CheqroomCheckoutResult>(HttpMethod.Get, $"api/cheqroom/quick-checkout?assetTag={assetTag}&userId={userId}",
               onError: onError);

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

        }

        public async Task ForceCheckIn(string deviceId, ErrorAction onError)
        {
            
            var openOrders = await GetOpenOrders(onError);
            if (!openOrders.Any(order => order.items.Contains(deviceId)))
                return;

            var order = openOrders.First(order => order.items.Contains(deviceId));
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
    }