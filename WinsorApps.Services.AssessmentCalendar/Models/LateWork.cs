using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.AssessmentCalendar.Models;

public readonly record struct NewLateWork(string details, string[] studentIds);

    public readonly record struct LateWorkDetails(string id, UserRecord student, string details, ImmutableArray<DateTime> markedDates,
        bool isResolved, DateTime? resolvedDate, bool isAssessment,
        SectionMinimalRecord? section = null, AssessmentEntryRecord? assessment = null);

    public readonly record struct LateWorkRecord(string id,
        string details, DateOnly markedDate, bool isResolved, DateTime? resolvedDate, bool isAssessment,
        string sectionId, string? assessmentId = null);

    public readonly record struct StudentLateWorkCollection(StudentRecordShort student, ImmutableArray<LateWorkRecord> lateWork)
    {
        public static implicit operator StudentLateWorkCollection(KeyValuePair<UserRecord, IEnumerable<LateWorkRecord>> kvp)
            => new(kvp.Key, kvp.Value.ToImmutableArray());

        public void Split(out StudentLateWorkCollection assessments, out StudentLateWorkCollection patterns)
        {
            assessments = new(student, lateWork.Where(lw => lw.isAssessment).ToImmutableArray());
            patterns = new(student, lateWork.Where(lw => !lw.isAssessment).ToImmutableArray());
        }
    }

    public readonly record struct LateWorkByStudentCollection(ImmutableArray<StudentLateWorkCollection> lateWorkByStudent)
    {
        public static implicit operator LateWorkByStudentCollection(List<KeyValuePair<UserRecord, IEnumerable<LateWorkRecord>>> dictionary)
            => new(dictionary.Select(kvp => (StudentLateWorkCollection)kvp).ToImmutableArray());
    }

    public static partial class Extensions
    {

        public static Dictionary<string, List<StudentLateWorkCollection>> SeparateByAssessmentId(this IEnumerable<StudentLateWorkCollection> collections)
        {
            Dictionary<string, List<StudentLateWorkCollection>> output = new();
            foreach (var col in collections)
            {
                var assessmentIds = col.lateWork
                    .Where(lw => !string.IsNullOrEmpty(lw.assessmentId))
                    .Select(lw => lw.assessmentId!)
                    .Distinct();
                foreach(var id in assessmentIds)
                {
                    if (!output.ContainsKey(id))
                        output.Add(id, new());
                    output[id].Add(new(col.student, 
                        col.lateWork.Where(lw => !string.IsNullOrEmpty(lw.assessmentId) && lw.assessmentId == id).ToImmutableArray()));
                }
            }

            return output;
        }
    }