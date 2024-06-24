using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.EventForms.Models;

namespace WinsorApps.Services.EventForms.Services;

public partial class EventFormsService
{
    public async Task<FacilitiesEvent?> GetFacilitiesEvent(string eventId, ErrorAction onError) =>
       await _api.SendAsync<FacilitiesEvent?>(HttpMethod.Get, $"api/events/{eventId}/facilities", onError: onError);


}
