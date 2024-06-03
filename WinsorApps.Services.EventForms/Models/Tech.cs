using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinsorApps.Services.EventForms.Models;
public readonly record struct NewVirtualEvent(bool webinar, bool registration, bool chatEnabled,
        bool qaEnabled, string qaSupportPerson, bool recording, bool reminder, bool transcript,
        bool registrantList, bool zoomLink, string hostContactId, ImmutableArray<string> panelistIds);

public readonly record struct NewTechEvent(bool presence, bool equipment, bool help, string details, NewVirtualEvent? virtualEvent = null);

public readonly record struct VirtualEvent(bool webinar, bool registration, bool chatEnabled,
    bool qaEnabled, string qaSupportPerson, bool recording, bool reminder, bool transcript,
    bool registrantList, bool zoomLink, Contact? hostContact, ImmutableArray<Contact> panelists);

public readonly record struct TechEvent(string id, bool presence, bool equipment, bool help, string details, VirtualEvent? virtualEvent = null);