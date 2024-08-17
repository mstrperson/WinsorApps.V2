using System.Collections.Immutable;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Services.Admin;

public partial class EventsAdminService
{
    public async Task<EventFormBase?> ApproveRoomUse(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Put, $"api/events/{eventId}/approve-room-use", onError: onError);
        if (result.HasValue)
        {
            ComputeChangesAndUpdates([result.Value]);
        }

        return result;
    }

    public async Task<ImmutableArray<EventFormBase>> GetRoomPendingEvents(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<EventFormBase>?>(HttpMethod.Get, "api/events/pending-room-approval", onError: onError);
        if (result.HasValue)
        {
            ComputeChangesAndUpdates(result.Value);
        }
        return result ?? [];
    }

    public async Task<EventFormBase?> RevokeRoomUse(string eventId, NoteRecord note, ErrorAction onError)
    {
        var result = await _api.SendAsync<NoteRecord, EventFormBase?>(HttpMethod.Put, $"api/events/{eventId}/revoke-room-approval", note, onError: onError);
        if (result.HasValue)
        {
            ComputeChangesAndUpdates([result.Value]);
        }

        return result;
    }
}
