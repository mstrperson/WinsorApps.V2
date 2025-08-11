using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.AssessmentCalendar.Models;

public record AssessmentCalendarEvent
(string id, string type, string summary, string description,
    DateTime start, DateTime end, bool allDay, List<string> affectedClasses, bool? passUsed = null, bool? passAvailable = null)
{
    public static readonly AssessmentCalendarEvent Empty = new("", AssessmentType.Assessment, "", "", DateTime.Now, DateTime.Now, true, [], false, false);
}

public record StudentCalendarCollection(string studentId, 
    List<AssessmentCalendarEvent> assessmentCalendar);

public record CreateAssessmentCalendarNote(string note, List<string> classesAffected, DateOnly date)
{
    public static implicit operator CreateAssessmentCalendarNote(DayNote note) => new(note.note, note.affectedClasses, note.date);
}

public record CreateAPExam(string courseName, DateTime startDateTime, DateTime endDateTime,
    List<string> sectionIds, List<string> studentIds);

public record APExamDetail(string id, string courseName, DateTime startDateTime, DateTime endDateTime,
    string creatorId, List<string> sectionIds, List<string> studentIds)
{
    public static readonly APExamDetail Empty = new("", "", DateTime.Now, DateTime.Now, "", [], []);
}

public record APExamSectionConflict(SectionRecord section, StudentRecordShort[] studentsInExam);
public record APExamSectionConflictReport(string examId, APExamSectionConflict[] conflicts);

public record AssessmentType
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