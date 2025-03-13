using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.AssessmentCalendar.Models;

public record AssessmentConflictRecord(string studentId, List<string> assessments);

    public record AssessmentConflictsByDate(DateTime date, List<AssessmentConflictRecord> conflicts)
    {
        public static AssessmentConflictsByDate FromKVP(KeyValuePair<DateTime, IEnumerable<AssessmentConflictRecord>> pair)
            => new(pair.Key, pair.Value.ToList());
    }

    /// <summary>
    /// Information Used for Creating a new Assessment on the Assessment Calendar
    /// </summary>
    /// <param name="sectionDates">Dictionary containing Section Ids and the date for this assessment.</param>
    /// <param name="note">Additional info about the assessment.</param>
    public record CreateAssessmentRecord(List<AssessmentDateRecord> sectionDates, string note, bool doItAnyway = false);

    public record AssessmentDateRecord(string sectionId, DateTime date);

/// <summary>
/// Details of an assessment scheduled on a particular day for a class.
/// </summary>
/// <param name="assessmentId">Id of the assessment (use this if you are submitting a pass)</param>
/// <param name="section">The class details.</param>
/// <param name="assessmentDateTime">Date and Time when the assessment is scheduled (this is your class period)</param>
/// <param name="studentsUsingPasses">List of students who have used a Pass for this assessment.</param>
/// <param name="studentConflicts">
/// List of students with other assessments scheduled on the same day and how many assessments they have.
/// </param>
public record AssessmentEntryRecord(string groupId, string assessmentId, SectionRecord section, DateTime assessmentDateTime,
    List<AssessmentPassListItem> studentsUsingPasses, List<StudentConflictCount> studentConflicts, 
    List<StudentRecordShort> studentsWithPassAvailable, DateTime submitted)
{
    public static readonly AssessmentEntryRecord Empty = new("", "", SectionRecord.Empty, DateTime.Now, [], [], [], DateTime.MaxValue);

    public AssessmentCalendarEvent ToCalendarEvent(AssessmentGroup group) => 
        new(assessmentId, AssessmentType.Assessment, group.course, group.note, assessmentDateTime, assessmentDateTime.AddMinutes(75), false, []);
}

    public record StudentAssessmentRosterEntry(StudentRecordShort student, bool latePass, int conflictCount)
    {
        public bool redFlag => conflictCount > 1 && !latePass;
    }
    public record AssessmentEntryShort(string assessmentId, string displayName, string block, DateTime assessmentDateTime,
        AdvisorRecord teacher, List<StudentAssessmentRosterEntry> students)
    {
        public static implicit operator AssessmentEntryShort(AssessmentEntryRecord assessment) =>
            new(assessment.assessmentId, assessment.section.displayName, assessment.section.block,
                assessment.assessmentDateTime, assessment.section.teachers.First(),
                assessment.section.students.Select(student =>
                    new StudentAssessmentRosterEntry(student,
                        assessment.studentsUsingPasses.Any(pass => pass.student.id == student.id),
                        assessment.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.id)?.count ?? 0)
                ).ToList());
    }

    public record StudentConflictCount(StudentRecordShort student, int count, bool latePass, bool redFlag, List<string> assessmentIds)
{
    public static readonly StudentConflictCount Empty = new(UserRecord.Empty, 0, false, false, []);
}

    

    public record AssessmentPassListItem(StudentRecordShort student, DateTime timeStamp);

    /// <summary>
    /// An assessment for a course with several sections
    /// </summary>
    /// <param name="id">Assessment Id (use this when requesting a pass)</param>
    /// <param name="note">Teacher note about the assessment.</param>
    /// <param name="assessments">Details for each section of the class doing this assessment.</param>
    public record AssessmentGroup(string id, string course, string courseId, string note,
        List<AssessmentEntryRecord> assessments);