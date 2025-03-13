using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;

namespace WinsorApps.Services.EventForms.Services;

public partial class EventFormsService
{
    public async Task<MarCommRequest?> GetMarCommRequest(string eventId, ErrorAction onError) => await _api.SendAsync<MarCommRequest?>(HttpMethod.Get,
            $"api/events/{eventId}/marcom", onError: onError);

    public async Task<MarCommRequest?> PostMarComRequest(string eventId, NewMarCommRequest newRequest, ErrorAction onError)
    {
        var result = await _api.SendAsync<NewMarCommRequest, MarCommRequest?>(HttpMethod.Post,
            $"api/events/{eventId}/marcom", newRequest, onError: onError);
        if(result is not null)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasMarCom = true };
            EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }

    public async Task<bool> DeleteMarCommRequest(string eventId, ErrorAction onError)
    {
        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}/marcom", onError: err =>
        {
            success = false;
            onError(err);
        });

        if(success)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasMarCom = false };
            EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return success;
    }
}
