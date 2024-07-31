using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.AssessmentCalendar.Models;

public readonly record struct AssessmentConflictRecord(string studentId, ImmutableArray<string> assessments);

    public readonly record struct AssessmentConflictsByDate(DateTime date, ImmutableArray<AssessmentConflictRecord> conflicts)
    {
        public static AssessmentConflictsByDate FromKVP(KeyValuePair<DateTime, IEnumerable<AssessmentConflictRecord>> pair)
            => new(pair.Key, pair.Value.ToImmutableArray());
    }

    /// <summary>
    /// Information Used for Creating a new Assessment on the Assessment Calendar
    /// </summary>
    /// <param name="sectionDates">Dictionary containing Section Ids and the date for this assessment.</param>
    /// <param name="note">Additional info about the assessment.</param>
    public readonly record struct CreateAssessmentRecord(ImmutableArray<AssessmentDateRecord> sectionDates, string note, bool doItAnyway = false);

    public readonly record struct AssessmentDateRecord(string sectionId, DateTime date);

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
    public readonly record struct AssessmentEntryRecord(string assessmentId, SectionRecord section, DateTime assessmentDateTime,
        ImmutableArray<AssessmentPassListItem> studentsUsingPasses, ImmutableArray<StudentConflictCount> studentConflicts);

    public readonly record struct StudentAssessmentRosterEntry(StudentRecordShort student, bool latePass, int conflictCount)
    {
        public bool redFlag => conflictCount > 1 && !latePass;
    }
    public readonly record struct AssessmentEntryShort(string assessmentId, string displayName, string block, DateTime assessmentDateTime,
        AdvisorRecord teacher, ImmutableArray<StudentAssessmentRosterEntry> students)
    {
        public static implicit operator AssessmentEntryShort(AssessmentEntryRecord assessment) =>
            new AssessmentEntryShort(assessment.assessmentId, assessment.section.displayName, assessment.section.block,
                assessment.assessmentDateTime, assessment.section.teachers.First(),
                assessment.section.students.Select(student =>
                    new StudentAssessmentRosterEntry(student,
                        assessment.studentsUsingPasses.Any(pass => pass.student.id == student.id),
                        assessment.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.id).conflictCount)
                ).ToImmutableArray());
    }

    public readonly record struct StudentConflictCount(StudentRecordShort student, int conflictCount, bool latePass, bool redFlag);

    public readonly record struct AssessmentPassListItem(StudentRecordShort student, DateTime timeStamp);

    /// <summary>
    /// An assessment for a course with several sections
    /// </summary>
    /// <param name="id">Assessment Id (use this when requesting a pass)</param>
    /// <param name="note">Teacher note about the assessment.</param>
    /// <param name="assessments">Details for each section of the class doing this assessment.</param>
    public readonly record struct AssessmentGroup(string id, string course, string courseId, string note,
        ImmutableArray<AssessmentEntryRecord> assessments);