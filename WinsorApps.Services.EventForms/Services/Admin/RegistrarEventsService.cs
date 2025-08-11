using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Services.Admin;

public partial class EventsAdminService
{
    public async Task<EventFormBase?> ApproveRoomUse(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Put, $"api/events/{eventId}/approve-room-use", onError: onError);
        if (result is not null)
        {
            await ComputeChangesAndUpdates([result]);
        }

        return result;
    }

    public async Task<List<EventFormBase>> GetRoomPendingEvents(ErrorAction onError)
    {
        var result = await _api.SendAsync<List<EventFormBase>?>(HttpMethod.Get, "api/events/pending-room-approval", onError: onError);
        if (result is not null)
        {
            await ComputeChangesAndUpdates(result);
        }
        return result ?? [];
    }

    public async Task<EventFormBase?> RevokeRoomUse(string eventId, NoteRecord note, ErrorAction onError)
    {
        var result = await _api.SendAsync<NoteRecord, EventFormBase?>(HttpMethod.Put, $"api/events/{eventId}/revoke-room-approval", note, onError: onError);
        if (result is not null)
        {
            await ComputeChangesAndUpdates([result]);
        }

        return result;
    }
}
