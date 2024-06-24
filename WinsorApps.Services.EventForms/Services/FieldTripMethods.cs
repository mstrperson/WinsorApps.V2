using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.EventForms.Models;

namespace WinsorApps.Services.EventForms.Services;

public partial class EventFormsService
{
    public async Task<FieldTripDetails?> GetFieldTripDetails(string eventId, ErrorAction onError) =>
        await _api.SendAsync<FieldTripDetails?>(HttpMethod.Get, $"api/events/{eventId}/field-trip/detail", onError: onError);

    public async Task<FieldTripDetails?> PostFieldTripDetails(string eventId, NewFieldTrip fieldTrip, ErrorAction onError)
    {
        var result = await _api.SendAsync<NewFieldTrip, FieldTripDetails?>(HttpMethod.Post,
            $"api/events/{eventId}/field-trip", fieldTrip, onError:  onError);
        if(result.HasValue)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasFieldTripInfo = true };
            EventsCache = EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        return result;
    }

    public async Task<bool> DeleteFieldTripDetails(string eventId, ErrorAction onError)
    {
        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}/field-trip", onError: err =>
        {
            success = false;
            onError(err);
        });

        if(success)
        {
            var evt = EventsCache.First(e => e.id == eventId);
            var updated = evt with { hasFieldTripInfo = false };
            EventsCache = EventsCache.Replace(evt, updated);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return success;
    }
}
