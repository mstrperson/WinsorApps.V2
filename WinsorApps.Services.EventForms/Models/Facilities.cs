using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Models;
public record NewLocationSetup(string locationId, string instructions, bool includeSandwichSign = false, DateTime setupTime = default)
{
    public bool RemoveLocation => locationId.StartsWith('-');
}

public record LocationSetupInstructions(string locationId, string instructions, bool includeSandwichSign, DateTime setupTime);

public record NewFacilitiesEvent(bool setup, bool presence, bool breakdown, bool overnight, bool parking,
    List<NewLocationSetup> locationSetups);

public record FacilitiesEvent(string id, bool setup, bool presence, bool breakdown, bool parking, bool overnight,
    List<LocationSetupInstructions> locations, List<DocumentHeader> documents)
{
    public static readonly FacilitiesEvent Empty = new("", false, false, false, false, false, [], []);
}
