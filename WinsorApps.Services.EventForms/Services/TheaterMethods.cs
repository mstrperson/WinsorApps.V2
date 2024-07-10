using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Services;

public partial class EventFormsService
{
    public async Task<TheaterEvent?> GetTheaterDetails(string eventId, ErrorAction onError) =>
        await _api.SendAsync<TheaterEvent?>(HttpMethod.Get, $"api/events/{eventId}/theater", onError: onError);

    public async Task<TheaterEvent?> PostTheaterDetails(string eventId, NewTheaterEvent newTheater, ErrorAction onError)
    {
        var result = await _api.SendAsync<NewTheaterEvent, TheaterEvent?>(HttpMethod.Post,
            $"api/events/{eventId}/theater", newTheater, onError: onError);

        if(result.HasValue)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasTheaterRequest = true };
            EventsCache = EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        return result;
    }

    public async Task<bool> DeleteTheaterDetails(string eventId, ErrorAction onError)
    {
        bool success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}/theater", onError: err =>
        {
            success = false;
            onError(err);
        });

        if(success)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasTheaterRequest = false };
            EventsCache = EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return success;
    }

    public async Task<byte[]> GetTheaterAttachement(string eventId, string documentId, ErrorAction onError) => await _api.DownloadFile($"api/events/{eventId}/theater/documents/{documentId}", onError: onError);
    
    public async Task<bool> PostTheaterAttachment(string eventId, DocumentHeader header, byte[] fileContent, ErrorAction onError)
    {
        // TODO: fix the API Endpoint...

        return false;
    }
}