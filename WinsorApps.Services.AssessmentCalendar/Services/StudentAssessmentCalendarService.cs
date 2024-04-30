using System.Collections.Immutable;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.AssessmentCalendar.Services;

public partial class StudentAssessmentService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public StudentAssessmentService(ApiService api, LocalLoggingService logging)
        {
            _api = api;
            _logging = logging;
        }

        public async Task<ImmutableArray<AssessmentCalendarEvent>> GetMyCalendarOn(DateTime date, ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get, $"api/assessment-calendar/my-calendar?date",
                onError: onError);

        public async Task<ImmutableArray<AssessmentCalendarEvent>> GetMyCalendarInRange(ErrorAction onError, DateTime start = default, DateTime end = default)
        {
            if (start == default) { start = DateTime.Today; }
            var param = end == default ? "" : $"&end={end:yyyy-MM-dd}";
            return await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get, 
                $"api/assessment-calendar/my-calendar?start={start:yyyy-MM-dd}{param}", 
                onError: onError);
        }

        public async Task<ImmutableArray<AssessmentPassDetail>> GetMyPasses(ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<AssessmentPassDetail>>(HttpMethod.Get, "api/assessment-calendar/students/passes", onError: onError);

        public async Task WithdrawLatePass(string assessmentId, ErrorAction onError) =>
            await _api.SendAsync(HttpMethod.Delete, $"api/assessment-calendar/students/passes/{assessmentId}", 
                onError: onError);

        public async Task<AssessmentPass?> RequestLatePass(string assessmentId, ErrorAction onError) =>
            await _api.SendAsync<AssessmentPass?>(HttpMethod.Post, $"api/assessment-calendar/students/passes/{assessmentId}",
                onError: onError);


    }