using System.Collections.Immutable;

namespace WinsorApps.Services.AssessmentCalendar.Models;

public readonly record struct AssessmentCalendarEvent
(string id, string type, string summary, string description,
    DateTime start, DateTime end, bool allDay, ImmutableArray<string> affectedClasses, bool? passUsed = null, bool? passAvailable = null)
{
    public static readonly AssessmentCalendarEvent Empty = new("", AssessmentType.Assessment, "", "", DateTime.Now, DateTime.Now, true, [], false, false);
}

public readonly record struct StudentCalendarCollection(string studentId, 
    ImmutableArray<AssessmentCalendarEvent> assessmentCalendar);

public readonly record struct CreateAssessmentCalendarNote(string note, ImmutableArray<string> classesAffected, DateOnly date)
{
    public static implicit operator CreateAssessmentCalendarNote(DayNote note) => new(note.note, note.affectedClasses, note.date);
}

public readonly record struct CreateAPExam(string courseName, DateTime startDateTime, DateTime endDateTime,
    ImmutableArray<string> sectionIds, ImmutableArray<string> studentIds);

public readonly record struct APExamDetail(string id, string courseName, DateTime startDateTime, DateTime endDateTime,
    string creatorId, ImmutableArray<string> sectionIds, ImmutableArray<string> studentIds)
{
    public static readonly APExamDetail Empty = new("", "", DateTime.Now, DateTime.Now, "", [], []);
}

public readonly record struct AssessmentType
{
    public static readonly AssessmentType Assessment = new("assessment");
    public static readonly AssessmentType ApExam = new("ap-exam");
    public static readonly AssessmentType Note = new("note");
    public static readonly AssessmentType AthleticsDismissal = new("athletics-dismissal");
    public static readonly AssessmentType None = new("none");

    public static implicit operator string(AssessmentType type) => type._type;
    public static implicit operator AssessmentType(string str) => str.ToLowerInvariant() switch
    {
        "assessment" => Assessment,
        "ap-exam" => ApExam,
        "note" => Note,
        "athletics-dismissal" => AthleticsDismissal,
        _ => None
    };

    private readonly string _type;

    private AssessmentType(string type)
    {
        _type = type;
    }
}