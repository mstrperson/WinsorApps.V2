using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.AssessmentCalendar.Models;

public readonly record struct AssessmentCalendarEvent
(string id, string type, string summary, string description,
    DateTime start, DateTime end, bool allDay, ImmutableArray<string> affectedClasses, bool? passUsed = null, bool? passAvailable = null);

public readonly record struct StudentCalendarCollection(string studentId, 
    ImmutableArray<AssessmentCalendarEvent> assessmentCalendar);

public readonly record struct CreateAssessmentCalendarNote(string note, ImmutableArray<string> classesAffected, DateOnly date)
{
    public static implicit operator CreateAssessmentCalendarNote(DayNote note) => new(note.note, note.affectedClasses, note.date);
}

public readonly record struct CreateAPExam(string courseName, DateTime startDateTime, DateTime endDateTime,
    ImmutableArray<string> sectionIds, ImmutableArray<string> studentIds);

public readonly record struct APExamDetail(string id, string courseName, DateTime startDateTime, DateTime endDateTime,
    string creatorId, ImmutableArray<string> sectionIds, ImmutableArray<string> studentIds);

public readonly record struct AssessmentType
{
    public static readonly AssessmentType Assessment = new("assessment");
    public static readonly AssessmentType ApExam = new("ap-exam");
    public static readonly AssessmentType Note = new("note");

    public static implicit operator string(AssessmentType type) => type._type;
    public static implicit operator AssessmentType(string str) => str.ToLowerInvariant() switch
    {
        "assessment" => Assessment,
        "ap-exam" => ApExam,
        "note" => Note,
        _ => throw new InvalidCastException($"{str} is not a valid assessment type.")
    };

    private readonly string _type;

    private AssessmentType(string type)
    {
        _type = type;
    }
}