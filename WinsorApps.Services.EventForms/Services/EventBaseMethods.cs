using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;

namespace WinsorApps.Services.EventForms.Services;

public partial class EventFormsService
{
    public async Task<List<EventFormBase>> GetMyCreatedEvents(DateTime startDate, DateTime endDate, ErrorAction onError, bool updateCache = false)
    {
        CheckDefaults(ref startDate, ref endDate);

        updateCache = updateCache || startDate < CacheStartDate || endDate > CacheEndDate;

        if (updateCache)
        {
            await UpdateCache(startDate, endDate, onError);
        }

        return [.. EventsCache.Where(evt => evt.start >= startDate && evt.end <= endDate && evt.creatorId == _api.AuthUserId)];
    }

    private static void CheckDefaults(ref DateTime startDate, ref DateTime endDate)
    {
        if (startDate == default)
            startDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);

        if (endDate == default)
            endDate = startDate.AddMonths(1);

        if (startDate > endDate)
            (startDate, endDate) = (endDate, startDate);
    }

    public async Task<List<EventFormBase>> GetMyLeadEvents(DateTime startDate, DateTime endDate, ErrorAction onError, bool updateCache = false)
    {
        CheckDefaults(ref startDate, ref endDate);

        updateCache = updateCache || startDate < CacheStartDate || endDate > CacheEndDate;

        if (updateCache)
        {
            await UpdateCache(startDate, endDate, onError);
        }

        return [.. EventsCache.Where(evt => evt.start >= startDate && evt.end <= endDate && evt.leaderId == _api.AuthUserId)];
    }

    public async Task UpdateCache(DateTime startDate, DateTime endDate, ErrorAction onError)
    {
        var result = await _api.SendAsync<List<EventFormBase>?>(HttpMethod.Get,
                $"api/events/created?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}", onError: onError) ?? [];

        result = [.. result
            .Union(await _api.SendAsync<List<EventFormBase>?>(HttpMethod.Get,
                $"api/events/lead?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}", onError: onError) ?? [])
            .Distinct()];

        foreach (var evt in result)
        {
            if (EventsCache.Any(e => e.id == evt.id))
            {
                var old = EventsCache.First(e => e.id == evt.id);
                EventsCache.Replace(old, evt);
            }
            else
            {
                EventsCache.Add(evt);
            }
        }
    }

    public async Task<EventFormBase?> StartNewForm(NewEvent newEvent, ErrorAction onError)
    {
        var result = await _api.SendAsync<NewEvent, EventFormBase?>(HttpMethod.Post,
            "api/events", newEvent, onError: onError);

        if(result is not null)
        {
            EventsCache.Add(result);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }

    public async Task<EventFormBase?> UpdateEvent(string eventId, NewEvent updatedEvent, ErrorAction onError)
    {
        var result = await _api.SendAsync<NewEvent, EventFormBase?>(HttpMethod.Put,
            $"api/events/{eventId}", updatedEvent, onError: onError);

        if (result is not null)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            EventsCache.Replace(evt, result);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }

    public async Task<bool> DeleteEventForm(string eventId, ErrorAction onError)
    {
        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}", onError: err =>
        {
            success = false;
            onError(err);
        });

        if(success)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            EventsCache.Remove(evt);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return success;
    }


    public async Task<EventFormBase?> CompleteSubmission(string id, ErrorAction onError)
    {
        var existing = EventsCache.FirstOrDefault(evt => evt.id == id);
        var success = true;
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{id}/complete-submission", onError: err =>
        {
            success = false;
            onError(err);
        });

        if (result is null)
            return null;

        if (success)
        {
            if(existing is not null)
                EventsCache.Replace(existing, result);
            else
                EventsCache.Add(result);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }

    public async Task<EventFormBase?> CompleteUpdate(string id, ErrorAction onError)
    {
        var existing = EventsCache.FirstOrDefault(evt => evt.id == id);
        var success = true;
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{id}/complete-update", onError: err =>
        {
            success = false;
            onError(err);
        });

        if (result is null)
            return null;

        if (success)
        {
            if (existing is not null)
                EventsCache.Replace(existing, result);
            else
                EventsCache.Add(result);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }

    public async Task<EventFormBase> GetEvent(string id, ErrorAction onError, bool ignoreCache = false)
    {
        if (!ignoreCache && EventsCache.Any(evt => evt.id == id))
            return EventsCache.First(evt => evt.id == id);

        var evt = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{id}", onError: onError);
        if (evt is null)
            return EventFormBase.Empty;

        if (!EventsCache.Any(e => e.id == evt.id))
        {
            EventsCache.Add(evt);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        return evt;
    }

    public async Task<byte[]> DownloadPdf(string eventId, ErrorAction onError)
    {
        var fileContent = await _api.DownloadFile($"api/events/{eventId}/pdf", onError: onError);
        if(fileContent is not null && fileContent.Length > 0)
        {
            return fileContent;
        }
        return [];
    }
}
