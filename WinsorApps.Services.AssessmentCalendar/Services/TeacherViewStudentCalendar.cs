using System.Collections.Immutable;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.AssessmentCalendar.Services;

public partial class TeacherAssessmentService
    {
        public async Task<ImmutableArray<AssessmentCalendarEvent>> GetAdviseeCalendarOn(string studentId, DateTime date, ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get, $"api/assessment-calendar/advisee/{studentId}?date={date:yyyy-MM-dd}",
                onError: onError);
        public async Task<ImmutableArray<AssessmentCalendarEvent>> GetAdviseeCalendarInRange(ErrorAction onError, string studentId, DateTime start = default, DateTime end = default)
        {
            if (start == default) { start = DateTime.Today; }
            string param = end == default ? "" : $"&end={end:yyyy-MM-dd}";
            return await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get, 
                $"api/assessment-calendar/advisee/{studentId}?start={start:yyyy-MM-dd}{param}", 
                onError: onError);
        }

        public async Task<ImmutableArray<StudentRecordShort>> GetMyStudentList(ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<StudentRecordShort>>(HttpMethod.Get, "api/assessment-calendar/teachers/student-calendars/list",
                onError: onError);


        public async Task<ImmutableArray<AssessmentCalendarEvent>> GetStudentCalendar(ErrorAction onError, string studentId, DateOnly start = default, DateOnly end = default)
        {
            if (start == default) { start = DateOnly.FromDateTime(DateTime.Today); }
            string param = end == default ? "" : $"&toDate={end:yyyy-MM-dd}";
            return await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get, 
                $"api/assessment-calendar/teachers/student-calendars/{studentId}?fromDate={start:yyyy-MM-dd}{param}", 
                onError: onError);
        }

        public async Task<ImmutableArray<AssessmentPassDetail>> GetStudentPassess(string studentId, ErrorAction onError, bool showPast = false) =>
            await _api.SendAsync<ImmutableArray<AssessmentPassDetail>>(HttpMethod.Get, $"api/assessment-calendar/teachers/student-calendars/{studentId}/passes?showPast={showPast}",
                onError: onError);

        public async Task WithdrawLatePassForStudent(string studentId, string assessmentId, ErrorAction onError) =>
            await _api.SendAsync(HttpMethod.Delete, $"api/assessment-calendar/teachers/student-calendars/{studentId}/passes/{assessmentId}",
                onError: onError);
        
        public async Task<AssessmentPass?> RequestPassForStudent(string studentId, string assessmentId, ErrorAction onError) =>
            await _api.SendAsync<AssessmentPass?>(HttpMethod.Post, $"api/assessment-calendar/teachers/student-calendars/{studentId}/passes/{assessmentId}",
                onError: onError);
    }