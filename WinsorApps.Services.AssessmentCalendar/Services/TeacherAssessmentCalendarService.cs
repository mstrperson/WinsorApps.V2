using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.AssessmentCalendar.Services;

public partial class TeacherAssessmentService : 
    IAsyncInitService,
    ICacheService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    private readonly ReadonlyCalendarService _calendar;

    private ImmutableArray<StudentRecordShort>? _myStudents;

    public ImmutableArray<StudentRecordShort> MyStudents
    {
        get
        {
            if (!Ready || !_myStudents.HasValue)
                throw new ServiceNotReadyException(_logging, "Cannot Retrieve MyStudents list yet.");

            return _myStudents.Value;
        }
    }

    private ImmutableArray<CourseRecord>? _courseList;
    public ImmutableArray<CourseRecord> CourseList
    {
        get
        {
            if (!Ready || !_courseList.HasValue)
                throw new ServiceNotReadyException(_logging, "Cannot Retrieve Course List");
            return _courseList.Value;
        }
    }

    private List<AssessmentGroup> _myAssessments = [];

    public event EventHandler? OnCacheRefreshed;

    public ImmutableArray<AssessmentGroup> MyAssessments
    {
        get
        {
            if (!Ready)
                throw new ServiceNotReadyException(_logging, "Cannot Retrieve MyAssessments yet.");
            return _myAssessments.ToImmutableArray();
        }

    }

    public bool Ready { get; private set; } = false;

    public bool Started { get; private set; }

    public double Progress { get; private set; }

    public TeacherAssessmentService(ApiService api, LocalLoggingService logging, ReadonlyCalendarService calendar)
    {
        _api = api;
        _logging = logging;
        _calendar = calendar;
    }

    public async Task Initialize(ErrorAction onError)
    {
        Started = true;

        var studentTask = GetMyStudentList(onError);
        studentTask.WhenCompleted(() =>
        {
            _myStudents = studentTask.Result;
            Progress += 0.25;
        });
        var courseTask = GetMyCourseList(onError);
        courseTask.WhenCompleted(() =>
        {
            _courseList = courseTask.Result;
            Progress += 0.25;
        });
        var assessmentTask = GetMyAssessments(onError, DateTime.Today, DateTime.Today.AddYears(1));
        assessmentTask.WhenCompleted(() =>
        {
            _myAssessments = assessmentTask.Result.ToList();
            Progress += 0.25;
        });

        var adviseeLateWork = GetAdviseesLateWork(onError, true);
        adviseeLateWork.WhenCompleted(() =>
        {
            _adviseeLateWork = adviseeLateWork.Result;
            Progress += 0.25;
        });

        await Task.WhenAll(studentTask, courseTask, assessmentTask, adviseeLateWork);

        Ready = true;
    }

    public async Task<ImmutableArray<AssessmentEntryRecord>> GetAssessmentsFor(ErrorAction onError, string sectionId, DateTime start = default, DateTime end = default)
    {
        var query = "";
        if (start != default)
        {
            query += $"?start={start:yyyy-MM-dd}";
            if (end != default)
                query += $"&end={end:yyyy-MM-dd}";
        }
        var result = await _api.SendAsync<ImmutableArray<AssessmentEntryRecord>>(HttpMethod.Get,
            $"api/assessment-calendar/teachers/sections/{sectionId}{query}",
            onError: onError);

        return result;
    }


    public async Task<ImmutableArray<AssessmentGroup>> GetMyAssessments(ErrorAction onError, DateTime start = default, DateTime end = default)
    {
        var query = "";
        if (start == default)
            return await _api.SendAsync<ImmutableArray<AssessmentGroup>>(HttpMethod.Get,
                "api/assessment-calendar/teachers",
                onError: onError);
        query += $"?start={start:yyyy-MM-dd}";
        if (end != default)
            query += $"&end={end:yyyy-MM-dd}";

        return await _api.SendAsync<ImmutableArray<AssessmentGroup>>(HttpMethod.Get, $"api/assessment-calendar/teachers{query}",
            onError: onError);
    }

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetCalendarByClassOn(string[] classes, DateTime date, ErrorAction onError)
    {
        var result = await _api.SendAsync<string[], ImmutableArray<AssessmentCalendarEvent>>(
            HttpMethod.Get,
            $"api/assessment-calendar/by-class?date={date:yyyy-MM-dd}",
            classes,
            onError: onError);

        _calendar.MergeNewAssessments(result);
        return result;
    }
    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetCalendarByClassInRange(ErrorAction onError, string[] classes, DateTime start = default, DateTime end = default)
    {
        if (start == default) { start = DateTime.Today; }
        var param = end == default ? "" : $"&end={end:yyyy-MM-dd}";
        var result = await _api.SendAsync<string[], ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get,
            $"api/assessment-calendar/by-class?start={start:yyyy-MM-dd}{param}",
            classes,
            onError: onError);
        _calendar.MergeNewAssessments(result);
        return result;
    }


    public async Task<AssessmentEntryRecord?> GetAssessmentDetails(string assessmentId, ErrorAction onError) =>
        await _api.SendAsync<AssessmentEntryRecord?>(HttpMethod.Get, $"api/assessment-calendar/teachers/{assessmentId}/assessment-details",
            onError: onError);

    public async Task<ImmutableArray<CourseRecord>> GetMyCourseList(ErrorAction onError) =>
        await _api.SendAsync<ImmutableArray<CourseRecord>>(HttpMethod.Get,
            "api/assessment-calendar/teachers/course-list",
            onError: onError);
    public async Task<ImmutableArray<SectionMinimalRecord>> GetMySectionsOf(string courseId, ErrorAction onError) =>
                await _api.SendAsync<ImmutableArray<SectionMinimalRecord>>(HttpMethod.Get,
                    $"api/assessment-calendar/teachers/courses/{courseId}/sections",
                    onError: onError);
    public async Task<AssessmentGroup?> GetMyCourseList(string assessmentId, ErrorAction onError) =>
        await _api.SendAsync<AssessmentGroup?>(HttpMethod.Get, $"api/assessment-calendar/teachers/{assessmentId}",
            onError: onError);
    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetMyCalendarInRange(ErrorAction onError, DateTime start = default, DateTime end = default)
    {
        if (start == default) { start = DateTime.Today; }
        var param = end == default ? "" : $"&end={end:yyyy-MM-dd}";
        var result = await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get,
            $"api/assessment-calendar/teachers?start={start:yyyy-MM-dd}{param}",
            onError: onError);
        _calendar.MergeNewAssessments(result);
        return result;
    }

    public async Task<AssessmentGroup?> CreateNewAssessment(CreateAssessmentRecord newAssessment, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateAssessmentRecord, AssessmentGroup?>(
            HttpMethod.Post, $"api/assessment-calendar/teachers", newAssessment,
            onError: onError);

        if (!result.HasValue) return result;
        _myAssessments.Add(result.Value);
        _calendar.Initialize(onError).SafeFireAndForget(e => e.LogException(_logging));
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);

        return result;
    }
    public async Task<AssessmentGroup?> UpdateAssessment(string groupId, CreateAssessmentRecord newAssessment, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateAssessmentRecord, AssessmentGroup?>(
            HttpMethod.Put, $"api/assessment-calendar/teachers/{groupId}", newAssessment,
            onError: onError);

        if (!result.HasValue) return result;

        var group = MyAssessments.FirstOrDefault(g => g.id == groupId);
        if (group != default)
            _myAssessments.Replace(group, result.Value);
        else
            _myAssessments.Add(result.Value);

        _calendar.Initialize(onError).SafeFireAndForget(e => e.LogException(_logging));
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);

        return result;
    }

    public async Task DeleteAssessment(string groupId, ErrorAction onError)
    {
        bool error = false;
        await _api.SendAsync(HttpMethod.Delete, $"api/assessment-calendar/teachers/{groupId}", onError: onError);

        if (!error)
        {
            var group = MyAssessments.FirstOrDefault(g => g.id == groupId);
            if (group != default)
                _myAssessments.Remove(group);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(100);
    }

    public async Task Refresh(ErrorAction onError)
    {
        await Initialize(onError);
    }
}