using System.Collections.Immutable;

namespace WinsorApps.Services.Global.Models;

public record CycleDayRecurringEvent(string id, DateOnly beginning, DateOnly ending, string creatorId,
        string summary, string description, List<string> attendees, bool allDay, TimeOnly time, int duration,
        List<string> cycleDays, int frequency, bool isPublic);

public record CreateRecurringEvent(DateOnly beginning, DateOnly ending, string summary, string description,
    List<string> attendees, List<string> cycleDays, int frequency,
    bool isPublic = false, bool allDay = false, TimeOnly time = default, int duration = 0);