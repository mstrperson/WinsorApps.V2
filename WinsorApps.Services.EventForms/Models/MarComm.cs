using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinsorApps.Services.EventForms.Models;
public readonly record struct NewMarCommRequest(bool printInvite, bool digitalInvite,
        bool newsletterReminder, bool emailReminder, bool scriptHelp, bool printedProgram, bool digitalProgram,
        bool needCreatedMedia, bool needPhotographer, ImmutableArray<string> inviteContactIds);

public readonly record struct MarCommRequest(string eventId, bool printInvite, bool digitalInvite,
    bool newsletterReminder, bool emailReminder, bool scriptHelp, bool printedProgram, bool digitalProgram,
    bool needCreatedMedia, bool needPhotographer, ImmutableArray<Contact> inviteList);
