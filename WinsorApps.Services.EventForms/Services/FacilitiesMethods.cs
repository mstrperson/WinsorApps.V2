using WinsorApps.Services.EventForms.Models;

namespace WinsorApps.Services.EventForms.Services;

public partial class EventFormsService
{
    public async Task<FacilitiesEvent?> GetFacilitiesEvent(string eventId, ErrorAction onError) =>
       await _api.SendAsync<FacilitiesEvent?>(HttpMethod.Get, $"api/events/{eventId}/facilities", onError: onError);

    public async Task<FacilitiesEvent?> PostFacilitiesEvent(string eventId, NewFacilitiesEvent newFacilities, ErrorAction onError)
    {
        var result = await _api.SendAsync<NewFacilitiesEvent, FacilitiesEvent?>(HttpMethod.Post,
            $"api/events/{eventId}/facilities", newFacilities, onError: onError);
        if(result.HasValue)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasFacilitiesInfo = true };
            EventsCache = EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }
    public async Task<bool> DeleteFacilitiesEvent(string eventId, ErrorAction onError)
    {
        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}/facilities", onError: err =>
        {
            success = false;
            onError(err);
        });

        if (success)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasFacilitiesInfo = false };
            EventsCache = EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return success;
    }
}
