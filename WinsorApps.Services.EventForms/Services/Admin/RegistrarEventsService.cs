using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Services.Admin;

public partial class EventsAdminService
{
    public async Task<EventFormBase?> ApproveRoomUse(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Put, $"api/events/{eventId}/approve-room-use", onError: onError);
        if (result.HasValue)
        {
            AllEvents.AddOrUpdate(result.Value.id, result.Value, (id, e) => e);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }
    public async Task<EventFormBase?> RevokeRoomUse(string eventId, NoteRecord note, ErrorAction onError)
    {
        var result = await _api.SendAsync<NoteRecord, EventFormBase?>(HttpMethod.Put, $"api/events/{eventId}/revoke-room-approval", note, onError: onError);
        if (result.HasValue)
        {
            AllEvents.AddOrUpdate(result.Value.id, result.Value, (id, e) => e);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }
}
