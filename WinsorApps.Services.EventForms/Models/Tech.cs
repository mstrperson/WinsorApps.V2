namespace WinsorApps.Services.EventForms.Models;
public record NewVirtualEvent(bool webinar, bool registration, bool chatEnabled,
        bool qaEnabled, string qaSupportPerson, bool recording, bool reminder, bool transcript,
        bool registrantList, bool zoomLink, string hostContactId, List<string> panelistIds);

public record NewTechEvent(bool presence, bool equipment, bool help, string details, NewVirtualEvent? virtualEvent = null);

public record VirtualEvent(bool webinar, bool registration, bool chatEnabled,
    bool qaEnabled, string qaSupportPerson, bool recording, bool reminder, bool transcript,
    bool registrantList, bool zoomLink, Contact? hostContact, List<Contact> panelists);

public record TechEvent(string id, bool presence, bool equipment, bool help, string details, VirtualEvent? virtualEvent = null)
{
    public static readonly TechEvent Empty = new("", false, false, false, "", null);
}