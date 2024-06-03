using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Models;
public readonly record struct NewLocationSetup(string locationId, string instructions, bool includeSandwichSign = false, DateTime setupTime = default)
{
    public bool RemoveLocation => locationId.StartsWith("-");
}

public readonly record struct LocationSetupInstructions(string locationId, string instructions, bool includeSandwichSign, DateTime setupTime);

public readonly record struct NewFacilitiesEvent(bool setup, bool presence, bool breakdown, bool overnight, bool parking,
    ImmutableArray<NewLocationSetup> locationSetups);

public readonly record struct FacilitiesEvent(string id, bool setup, bool presence, bool breakdown, bool parking, bool overnight,
    ImmutableArray<LocationSetupInstructions> locations, ImmutableArray<DocumentHeader> documents);
