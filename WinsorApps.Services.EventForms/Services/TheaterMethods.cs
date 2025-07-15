using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
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

        if(result is not null)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasTheaterRequest = true };
            EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        return result;
    }

    public async Task<bool> DeleteTheaterDetails(string eventId, ErrorAction onError)
    {
        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}/theater", onError: err =>
        {
            success = false;
            onError(err);
        });

        if(success)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasTheaterRequest = false };
            EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return success;
    }

    public async Task<byte[]> DownloadAttachment(string documentId, ErrorAction onError)
    {
        var result = await _api.DownloadFile($"api/events/attachments/{documentId}", onError: onError);
        _logging.LogMessage(Global.Services.LocalLoggingService.LogLevel.Information,
            $"Downloading attachment {documentId} yielded {result.Length} bytes");
        return result;
    }

    public async Task<bool> DeleteAttachment(string eventId, string documentId, ErrorAction onError)
    {
        var success = true;

        await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}/attachments/{documentId}", onError: 
            err =>
            {
                success = false; 
                onError(err);
            });

        if (success)
            _logging.LogMessage(Global.Services.LocalLoggingService.LogLevel.Information, $"Deleted Attachment {documentId} from event {eventId}.");

        return success;
    }

    public async Task<DocumentHeader?> UploadAttachment(string eventId, DocumentHeader header, byte[] fileContent, ErrorAction onError)
    {
        var result = await _api.UploadDocument($"api/events/{eventId}/file-upload", header, fileContent, onError);

        if(result is not null)
        {
            _logging.LogMessage(Global.Services.LocalLoggingService.LogLevel.Information,
                $"Uploaded {header.fileName} to event {eventId}.");
        }

        return result;
    }

    public async Task<byte[]> GetTheaterAttachement(string eventId, string documentId, ErrorAction onError) => 
        await _api.DownloadFile($"api/events/{eventId}/theater/documents/{documentId}", onError: onError);
    
    public async Task<DocumentHeader?> PostTheaterAttachment(string eventId, DocumentHeader header, byte[] fileContent, ErrorAction onError) => 
        await Task.FromResult<DocumentHeader?>(null); // Not implemented in the original code, but can be added similarly to UploadAttachment
}