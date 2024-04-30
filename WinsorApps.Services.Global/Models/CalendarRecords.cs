using System.Collections.Immutable;

namespace WinsorApps.Services.Global.Models;

public record struct CalendarEventRecord<T>(
    DateTime start,
    DateTime end,
    string summary,
    T details,
    UserRecord eventCreator,
    IEnumerable<LocationRecord> locations);

public record struct LocationRecord(string id, string label, string type);

public readonly record struct SchoolYear(
    string id,
    string label,
    ImmutableArray<TermRecord> terms,
    DateOnly startDate,
    DateOnly endDate,
    DateOnly seniorsLastDay);

public record struct CycleDay(DateOnly date, string cycleDay);