using System.Collections.Immutable;

namespace WinsorApps.Services.EventForms.Models;
public record NewMarCommRequest(bool printInvite, bool digitalInvite,
        bool newsletterReminder, bool emailReminder, bool scriptHelp, bool printedProgram, bool digitalProgram,
        bool needCreatedMedia, bool needPhotographer, List<string> inviteContactIds);

public record MarCommRequest(string eventId, bool printInvite, bool digitalInvite,
    bool newsletterReminder, bool emailReminder, bool scriptHelp, bool printedProgram, bool digitalProgram,
    bool needCreatedMedia, bool needPhotographer, List<Contact> inviteList)
{
    public static readonly MarCommRequest Empty = new("", false, false, false, false, false, false, false, false, false, []);
}
