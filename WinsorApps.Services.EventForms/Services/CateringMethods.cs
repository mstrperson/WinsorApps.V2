using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;

namespace WinsorApps.Services.EventForms.Services;

public partial class EventFormsService
{
    public async Task<CateringEvent?> GetCateringEvent(string id, ErrorAction onError)
    {
        var eventBase = await GetEvent(id, onError);
        if (string.IsNullOrEmpty(eventBase.id))
            return null;

        var cateringDetails = await _api.SendAsync<CateringEvent?>(HttpMethod.Get, $"api/events/{id}/catering", onError: onError);
        return cateringDetails;
    }

    public async Task<CateringEvent?> PostCateringDetails(string id, NewCateringEvent newDetails, ErrorAction onError)
    {
        var eventBase = await GetEvent(id, onError);

        if (string.IsNullOrEmpty(eventBase.id))
            return null;

        var cateringDetails = await _api.SendAsync<NewCateringEvent, CateringEvent?>(HttpMethod.Post, $"api/events/{id}/catering", newDetails, onError: onError);
        if(!eventBase.hasCatering)
        {
            var update = eventBase with { hasCatering = true };
            EventsCache.Replace(eventBase, update);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        return cateringDetails;
    }

    public async Task DeleteCateringDetails(string id, ErrorAction onError)
    {
        var eventBase = await GetEvent(id, onError);

        if (string.IsNullOrEmpty(eventBase.id))
            return;

        bool success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/{id}/catering", onError: err =>
        {
            success = false;
            onError(err);
        });

        if(success)
        {
            var updated = eventBase with { hasCatering = false };
        }
    }
}
