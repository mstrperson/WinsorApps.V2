using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.AssessmentCalendar.Models;

public record NewLateWork(string details, string[] studentIds);

    public record LateWorkDetails(string id, UserRecord student, string details, List<DateTime> markedDates,
        bool isResolved, DateTime? resolvedDate, bool isAssessment,
        SectionMinimalRecord? section = null, AssessmentEntryRecord? assessment = null);

    public record LateWorkRecord(string id,
        string details, DateOnly markedDate, bool isResolved, DateTime? resolvedDate, bool isAssessment,
        string sectionId, string? assessmentId = null);

public record StudentLateWorkCollection(StudentRecordShort student, List<LateWorkDetails> lateWork)
{
    public static implicit operator StudentLateWorkCollection(KeyValuePair<UserRecord, IEnumerable<LateWorkDetails>> kvp)
        => new(kvp.Key, [.. kvp.Value]);

    public void Split(out StudentLateWorkCollection assessments, out StudentLateWorkCollection patterns)
    {
        assessments = new(student, [.. lateWork.Where(lw => lw.isAssessment)]);
        patterns = new(student, [.. lateWork.Where(lw => !lw.isAssessment)]);
    }
}

    public record LateWorkByStudentCollection(List<StudentLateWorkCollection> lateWorkByStudent)
    {
        public static implicit operator LateWorkByStudentCollection(List<KeyValuePair<UserRecord, IEnumerable<LateWorkDetails>>> dictionary)
            => new([.. dictionary.Select(kvp => (StudentLateWorkCollection)kvp)]);
    }

public static partial class Extensions
{

    public static Dictionary<string, List<StudentLateWorkCollection>> SeparateByAssessmentId(this IEnumerable<StudentLateWorkCollection> collections)
    {
        Dictionary<string, List<StudentLateWorkCollection>> output = [];
        foreach (var col in collections)
        {
            var assessmentIds = col.lateWork
                .Where(lw => lw.assessment is not null)
                .Select(lw => lw.assessment!.assessmentId)
                .Distinct();
            foreach (var id in assessmentIds)
            {
                var asmt = output.GetOrAdd(id, []);
                asmt.Add(new(col.student,
                    [.. col.lateWork.Where(lw => 
                        !string.IsNullOrEmpty(lw.assessment!.assessmentId) && 
                        lw.assessment!.assessmentId == id)]));
            }
        }

        return output;
    }
}