using System.Collections.Immutable;

namespace WinsorApps.Services.AssessmentCalendar.Models;

public readonly record struct AssessmentCalendarEvent
(string id, string type, string summary, string description,
    DateTime start, DateTime end, bool allDay, ImmutableArray<string> affectedClasses, bool? passUsed = null, bool? passAvailable = null);

public readonly record struct StudentCalendarCollection(string studentId, 
    ImmutableArray<AssessmentCalendarEvent> assessmentCalendar);

public readonly record struct CreateAssessmentCalendarNote(string note, ImmutableArray<string> classesAffected, DateOnly date);

public readonly record struct CreateAPExam(string courseName, DateTime startDateTime, DateTime endDateTime,
    ImmutableArray<string> sectionIds, ImmutableArray<string> studentIds);

public readonly record struct APExamDetail(string id, string courseName, DateTime startDateTime, DateTime endDateTime,
    string creatorId, ImmutableArray<string> sectionIds, ImmutableArray<string> studentIds);