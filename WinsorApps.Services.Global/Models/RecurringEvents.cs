using System.Collections.Immutable;

namespace WinsorApps.Services.Global.Models;

public readonly record struct CycleDayRecurringEvent(string id, DateOnly beginning, DateOnly ending, string creatorId,
        string summary, string description, ImmutableArray<string> attendees, bool allDay, TimeOnly time, int duration,
        ImmutableArray<string> cycleDays, int frequency, bool isPublic);

public readonly record struct CreateRecurringEvent(DateOnly beginning, DateOnly ending, string summary, string description,
    ImmutableArray<string> attendees, ImmutableArray<string> cycleDays, int frequency,
    bool isPublic = false, bool allDay = false, TimeOnly time = default, int duration = 0);