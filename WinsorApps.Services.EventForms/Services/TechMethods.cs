using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;

namespace WinsorApps.Services.EventForms.Services;

public partial class EventFormsService
{
    public async Task<TechEvent?> GetTechDetails(string eventId, ErrorAction onError) => await _api.SendAsync<TechEvent?>(HttpMethod.Get,
        $"api/events/{eventId}/technology", onError: onError);
    public async Task<TechEvent?> PostTechEvent(string eventId, NewTechEvent tech, ErrorAction onError)
    {
        var result = await _api.SendAsync<NewTechEvent, TechEvent?>(HttpMethod.Post,
            $"api/events/{eventId}/technology", tech, onError: onError);
        if(result is not null)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasTechRequest = true };
            EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        return result;
    }
        
    /// <summary>
    /// returns true if the delete was successful.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="onError"></param>
    /// <returns></returns>
    public async Task<bool> DeleteTechEvent(string eventId, ErrorAction onError)
    {
        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}/technology", onError: err =>
        {
            success = false;
            onError(err);
        });

        if(success)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasTechRequest = false };
            EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return success;
    }

    public async Task<VirtualEvent?> GetVirtualEvent(string eventId, ErrorAction onError) =>
        await _api.SendAsync<VirtualEvent?>(HttpMethod.Get, $"api/events/{eventId}/technology/virtual-event", onError: onError);
        
    public async Task<VirtualEvent?> PostVirtualEvent(string eventId, NewVirtualEvent virtualEvent, ErrorAction onError)
    {
        var result = await _api.SendAsync<NewVirtualEvent, VirtualEvent?>(HttpMethod.Post,
            $"api/events/{eventId}/technology/virtual-event", virtualEvent, onError: onError);
        if (result is not null)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasTechRequest = true, hasZoom = true };
            EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        return result;
    }

    /// <summary>
    /// returns true if the details were successfully deleted.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="onError"></param>
    /// <returns></returns>
    public async Task<bool> DeleteVirtualEvent(string eventId, ErrorAction onError)
    {
        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}/technology/virtual-event", onError: err =>
        {
            success = false;
            onError(err);
        });

        if (success)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasZoom = false };
            EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return success;
    }
}
