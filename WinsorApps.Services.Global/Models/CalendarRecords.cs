namespace WinsorApps.Services.Global.Models;

public record CalendarEvent<T>(
    DateTime start,
    DateTime end,
    string summary,
    T details,
    UserRecord eventCreator,
    List<Location> locations);

public record Location(string id, string label, string type);

public record SchoolYear(
    string id,
    string label,
    List<TermRecord> terms,
    DateOnly startDate,
    DateOnly endDate,
    DateOnly seniorsLastDay)
{
    public static implicit operator string(SchoolYear schoolYear) => schoolYear.label;
}

public record CycleDay(DateOnly date, string cycleDay);