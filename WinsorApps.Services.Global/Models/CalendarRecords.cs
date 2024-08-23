using System.Collections.Immutable;

namespace WinsorApps.Services.Global.Models;

public record struct CalendarEvent<T>(
    DateTime start,
    DateTime end,
    string summary,
    T details,
    UserRecord eventCreator,
    IEnumerable<Location> locations);

public record struct Location(string id, string label, string type);

public readonly record struct SchoolYear(
    string id,
    string label,
    ImmutableArray<TermRecord> terms,
    DateOnly startDate,
    DateOnly endDate,
    DateOnly seniorsLastDay);

public record struct CycleDay(DateOnly date, string cycleDay);