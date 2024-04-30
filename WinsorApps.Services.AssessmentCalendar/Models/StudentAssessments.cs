using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.AssessmentCalendar.Models;

/// <summary>
    /// Generic Calendar event display for things on the assessment calendar.
    /// </summary>
    /// <param name="text">The display message associated with this event.</param>
    /// <param name="startDateTime">Start date (and time?)</param>
    /// <param name="endDateTime">(optional) end date and time, if applicable</param>
    /// <param name="allDay">is this all day or does it have relevant time data as well.</param>
    /// <param name="type">what kind of event is this?  [assessment, ap-exam, note]</param>
    /// <param name="id">item id (used only for assessments if you want to use a pass)</param>
    public readonly record struct AssessmentCalendarDisplayRecord(string text, DateTime startDateTime, DateTime endDateTime,
        bool allDay, string type, string id, ImmutableArray<string> affectedClasses)
    {
        public static implicit operator AssessmentCalendarEvent(AssessmentCalendarDisplayRecord record) =>
            new(record.id, record.type, record.text, 
                record.affectedClasses.DelimeteredList($"{Environment.NewLine}"), record.startDateTime, record.endDateTime, record.allDay, record.affectedClasses);

        public static implicit operator DayNote(AssessmentCalendarDisplayRecord record) =>
            new(record.id, DateOnly.FromDateTime(record.startDateTime), record.text, record.affectedClasses);
        
    }

    /// <summary>
    /// Assessment Details given to Student for Assessment Calendar viewing.
    /// </summary>
    /// <param name="courseName">Name of the class</param>
    /// <param name="assessmentDateTime">Date and Time (i.e. uses block information)</param>
    /// <param name="assessmentNote">Note about the assessment</param>
    /// <param name="assessmentId">hash encoded AssessmentCalendarEntry.GroupId used for requesting Late Pass</param>
    /// <param name="sectionId">hash encoded AssessmentCalendarEntry.SectionId used for requesting Late Pass</param>
    public readonly record struct StudentAssessmentDetailsRecord(
        string courseName, DateTime assessmentDateTime, string assessmentNote, string assessmentId, string sectionId);

    /// <summary>
    /// Note for students/teacher on the Assessment Calendar for a particular day.
    /// </summary>
    /// <param name="date">Date the note is set for</param>
    /// <param name="note">Message to display</param>
    /// <param name="affectedClasses">which classes are affected</param>
    public readonly record struct DayNote(string id, DateOnly date, string note, ImmutableArray<string> affectedClasses);

    /// <summary>
    /// Response to an assessment pass request.
    /// </summary>
    /// <param name="assessmentId">Assessment for which you are using a pass.</param>
    /// <param name="studentId">your id</param>
    /// <param name="timeStamp">when you submitted this pass</param>
    public readonly record struct AssessmentPass(string assessmentId, string studentId, DateTime timeStamp);

    public readonly record struct AssessmentPassDetail(StudentAssessmentDetailsRecord assessment, UserRecord student, DateTime timeStamp);

    /// <summary>
    /// Display an AP Exam
    /// </summary>
    /// <param name="courseName">AP Course Name</param>
    /// <param name="examStart">Date and Time the exam starts</param>
    /// <param name="examEnd">Time the exam ends</param>
    public readonly record struct APExamRecord(string courseName, DateTime examStart, DateTime examEnd);