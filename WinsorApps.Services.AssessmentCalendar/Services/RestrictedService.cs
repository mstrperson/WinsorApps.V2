using System.Collections.Immutable;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.AssessmentCalendar.Services;

public partial class AssessmentCalendarRestrictedService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;



        public AssessmentCalendarRestrictedService(ApiService api, LocalLoggingService logging, TeacherAssessmentService teacherService)
        {
            _api = api;
            _logging = logging;
        }

        public async Task<ImmutableArray<StudentLateWorkCollection>> GetLateWorkForAllStudents(ErrorAction onError, bool includeResolved = false) =>
            await _api.SendAsync<ImmutableArray<StudentLateWorkCollection>>(HttpMethod.Get, 
                $"api/assessment-calendar/late-work/all-students?includeResolved={includeResolved}",
                onError: onError);


        public async Task<ImmutableArray<AssessmentCalendarDisplayRecord>> GetStudentCalendar(ErrorAction onError, string studentId, DateTime start = default, DateTime end = default)
        {
            if (start == default) { start = DateTime.Today; }
            var param = end == default ? "" : $"&toDate={end:yyyy-MM-dd}";
            return await _api.SendAsync<ImmutableArray<AssessmentCalendarDisplayRecord>>(HttpMethod.Get, 
                $"api/assessment-calendar/students/{studentId}?fromDate={start:yyyy-MM-dd}{param}", onError: onError);
        }

        public async Task<ImmutableArray<AssessmentConflictsByDate>> GetConflicts(ErrorAction onError, DateTime start = default, DateTime end = default)
        {
            if (start == default) { start = DateTime.Today; }
            string param = end == default ? "" : $"&end={end:yyyy-MM-dd}";
            return await _api.SendAsync<ImmutableArray<AssessmentConflictsByDate>>(HttpMethod.Get, 
                $"api/assessment-calendar/assessment-conflicts?start={start:yyyy-MM-dd}{param}", onError: onError);
        }

        #region Notes

        public async Task<AssessmentCalendarDisplayRecord?> UpdateNote(string noteId, CreateAssessmentCalendarNote note, ErrorAction onError) =>
            await _api.SendAsync<CreateAssessmentCalendarNote, AssessmentCalendarDisplayRecord?>(HttpMethod.Put, 
                $"api/assessment-calendar/notes/{noteId}", note, onError: onError);

        public async Task<AssessmentCalendarDisplayRecord?> CreateNote(CreateAssessmentCalendarNote note, ErrorAction onError) =>
            await _api.SendAsync<CreateAssessmentCalendarNote, AssessmentCalendarDisplayRecord?>(HttpMethod.Post, 
                "api/assessment-calendar/notes", note, onError: onError);

        public async Task DeleteNote(string noteId, ErrorAction onError) =>
            await _api.SendAsync(HttpMethod.Delete, $"api/assessment-calendar/notes/{noteId}", onError: onError);


        public async Task<ImmutableArray<AssessmentCalendarDisplayRecord>> GetNotes(ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<AssessmentCalendarDisplayRecord>>(HttpMethod.Get, 
                "api/assessment-calendar/notes", onError: onError);

        #endregion // Notes
        #region AP Exams

        public async Task<APExamDetail?> UpdateAPExam(string examId, CreateAPExam exam, ErrorAction onError) =>
            await _api.SendAsync<CreateAPExam, APExamDetail>(HttpMethod.Put, 
                $"api/assessment-calendar/ap-exam/{examId}", exam, onError: onError);

        public async Task<AssessmentCalendarDisplayRecord?> CreateAPExam(CreateAPExam exam, ErrorAction onError) =>
            await _api.SendAsync<CreateAPExam, AssessmentCalendarDisplayRecord?>(HttpMethod.Post, 
                "api/assessment-calendar/ap-exam", exam, onError: onError);

        public async Task DeleteAP(string examId, ErrorAction onError) =>
            await _api.SendAsync(HttpMethod.Delete, $"api/assessment-calendar/ap-exam/{examId}", onError: onError);

        public async Task<APExamDetail?> GetAPExam(string id, ErrorAction onError) =>
            await _api.SendAsync<APExamDetail?>(HttpMethod.Get, $"api/assessment-calendar/ap-exam/{id}", onError: onError);

        public async Task<ImmutableArray<APExamDetail>> ApExams(ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<APExamDetail>>(HttpMethod.Get, $"api/assessment-calendar/ap-exam", onError: onError);

        #endregion // AP Exams
    }