using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using AsyncAwaitBestPractices;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.AssessmentCalendar.Services;

public partial class TeacherAssessmentService : 
    IAsyncInitService,
    IAutoRefreshingCacheService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    private readonly ReadonlyCalendarService _calendar;

    private ImmutableArray<StudentRecordShort> _myStudents = [];

    public ImmutableArray<StudentRecordShort> MyStudents
    {
        get
        {
            if (!Ready || _myStudents.IsEmpty)
                throw new ServiceNotReadyException(_logging, "Cannot Retrieve MyStudents list yet.");

            return _myStudents;
        }
    }

    private ImmutableArray<CourseRecord> _courseList = [];
    public ImmutableArray<CourseRecord> CourseList
    {
        get
        {
            if (!Ready || _courseList.IsEmpty)
                throw new ServiceNotReadyException(_logging, "Cannot Retrieve Course List");
            return _courseList;
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
            return [.. _myAssessments];
        }

    }
    public string CacheFileName => ".teacher-assessment-calendar.cache";

    private readonly record struct CacheStructure(
        ImmutableArray<StudentRecordShort> students,
        ImmutableArray<CourseRecord> courses,
        ImmutableArray<AssessmentGroup> assessments,
        ImmutableArray<StudentLateWorkCollection> lateWork,
        Dictionary<string, AssessmentEntryRecord> detailsCache);

    public void SaveCache()
    {
         var cache = new CacheStructure(_myStudents, _courseList, [.._myAssessments], _adviseeLateWork, AssessmentDetailsCache);
        try
        {
            File.WriteAllText($"{_logging.AppStoragePath}{CacheFileName}", JsonSerializer.Serialize(cache));
        }
        catch(Exception e)
        {
            e.LogException(_logging);
        }
    }

    public bool LoadCache()
    {
        if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
            return false;

        var cacheAge = DateTime.Now - File.GetCreationTime($"{_logging.AppStoragePath}{CacheFileName}");

        _logging.LogMessage(LocalLoggingService.LogLevel.Information,
            $"{CacheFileName} is {cacheAge.TotalDays:0.0} days old.");

        if (cacheAge.TotalDays > 14)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Deleting Aged Cache File.");
            File.Delete($"{_logging.AppStoragePath}{CacheFileName}");
            return false;
        }

        try
        {
            var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
            var cache = JsonSerializer.Deserialize<CacheStructure>(json);
            _myStudents = cache.students;
            _courseList = cache.courses;
            _myAssessments = [.. cache.assessments];
            _adviseeLateWork = cache.lateWork;
            AssessmentDetailsCache = cache.detailsCache ?? [];
        }
        catch
        {
            return false;
        }

        return true;
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

        if (!LoadCache())
        {
            await LoadManually(onError);
        }

        LoadCachedAssessmentDetails(onError).SafeFireAndForget(e => e.LogException(_logging));

        RefreshInBackground(CancellationToken.None, onError).SafeFireAndForget(e => e.LogException(_logging));

        Progress = 1;
        Ready = true;
    }

    private async Task LoadCachedAssessmentDetails(ErrorAction onError, bool reloadCache = false)
    {
        var assessmentIds = _calendar.AssessmentCalendar
            .Where(ent => ent.type == AssessmentType.Assessment)
            .Select(ent => ent.id)
            .ToImmutableArray();

        using DebugTimer __ = new($"Loading Cached assessment details for {assessmentIds.Length} assessments", _logging);
        _ = await GetAssessmentDetails(assessmentIds, onError, reloadCache);
    }

    private async Task LoadManually(ErrorAction onError)
    {
        var studentTask = GetMyStudentList(onError);
        studentTask.WhenCompleted(() =>
        {
            _myStudents = studentTask.Result;
            Progress += 0.25;
        },
        () =>
        {
            Progress += 0.25;
        });
        var courseTask = GetMyCourseList(onError);
        courseTask.WhenCompleted(() =>
        {
            _courseList = courseTask.Result;
            Progress += 0.25;
        },
        () =>
        {
            Progress += 0.25;
        });
        var assessmentTask = GetMyAssessments(onError, DateTime.Today, DateTime.Today.AddYears(1));
        assessmentTask.WhenCompleted(() =>
        {
            _myAssessments = [.. assessmentTask.Result];
            Progress += 0.25;
        },
        () =>
        {
            Progress += 0.25;
        });

        var adviseeLateWork = GetAdviseesLateWork(onError, true);
        adviseeLateWork.WhenCompleted(() =>
        {
            _adviseeLateWork = adviseeLateWork.Result;
            Progress += 0.25;
        },
        () =>
        {
            Progress += 0.25;
        });

        await Task.WhenAll(studentTask, courseTask, assessmentTask, adviseeLateWork);
        SaveCache();
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
        {
            var res = await _api.GetPagedResult<AssessmentGroup>(
                HttpMethod.Get,
                "api/assessment-calendar/teachers",
                onError: onError);

            if (res.ExceptBy(_myAssessments.Select(grp => grp.id), grp => grp.id).Any())
            {
                _myAssessments = _myAssessments.Merge(res, (oldgrp, newgrp) => oldgrp.id == newgrp.id);
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }

            return res;
        }
        query += $"?start={start:yyyy-MM-dd}";
        if (end != default)
            query += $"&end={end:yyyy-MM-dd}";

        var result = await _api.GetPagedResult<AssessmentGroup>(
            HttpMethod.Get,
            $"api/assessment-calendar/teachers{query}",
            onError: onError);

        return result;
    }

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetCalendarByClassOn(string[] classes, DateTime date, ErrorAction onError)
    {
        var result = await _api.GetPagedResult<string[], AssessmentCalendarEvent>(
            HttpMethod.Post,
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
        var result = await _api.GetPagedResult<string[], AssessmentCalendarEvent>(
            HttpMethod.Post,
            $"api/assessment-calendar/by-class?start={start:yyyy-MM-dd}{param}",
            classes,
            onError: onError);
        _calendar.MergeNewAssessments(result);
        return result;
    }

    public Dictionary<string, AssessmentEntryRecord> AssessmentDetailsCache { get; private set; } = [];

    public TimeSpan RefreshInterval => TimeSpan.FromMinutes(30);

    public bool Refreshing { get; private set; }

    public async Task<AssessmentEntryRecord?> GetAssessmentDetails(string assessmentId, ErrorAction onError, bool refresheCache = false)
    {
        if(!refresheCache && AssessmentDetailsCache.TryGetValue(assessmentId, out var details)) 
            return details;

        var result = await _api.SendAsync<AssessmentEntryRecord?>(HttpMethod.Get, $"api/assessment-calendar/teachers/{assessmentId}/assessment-details",
            onError: onError);

        if(result.HasValue)
        {
            if (!AssessmentDetailsCache.ContainsKey(assessmentId))
                AssessmentDetailsCache.Add(assessmentId, result.Value);
            else
                AssessmentDetailsCache[assessmentId] = result.Value;
        }
        SaveCache();
        return result;
    }

    public async Task<ImmutableArray<AssessmentEntryRecord>> GetAssessmentDetails(ImmutableArray<string> assessmentIds, ErrorAction onError, bool refresheCache = false)
    {
        var output = new List<AssessmentEntryRecord>();

        if(!refresheCache)
        {
            foreach(var id in assessmentIds)
            {
                if(AssessmentDetailsCache.TryGetValue(id, out var entry))
                    output.Add(entry);
            }

            assessmentIds = [.. assessmentIds.Except(output.Select(ent => ent.assessmentId))];
        }

        var result = await _api.GetPagedResult<ImmutableArray<string>, AssessmentEntryRecord>(
                HttpMethod.Post,
            $"api/assessment-calendar/teachers/assessment-details", assessmentIds,
            onError: onError);

        output = [.. output, .. result];
        foreach(var entry in result)
        {
            AssessmentDetailsCache.AddOrUpdate(entry.assessmentId, entry);
        }

        SaveCache();
        return [..output];
    }

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
        var result = await _api.GetPagedResult<AssessmentCalendarEvent>(HttpMethod.Get,
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

        if (!result.HasValue) 
            return result;

        _myAssessments.Add(result.Value);
        // TODO: This is awful. stop.
        _calendar.Refresh(onError).SafeFireAndForget(e => e.LogException(_logging));
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        SaveCache();
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

        // TODO: This is awful!! STOP
        _calendar.Refresh(onError).SafeFireAndForget(e => e.LogException(_logging));
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        SaveCache();
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
            SaveCache();
        }
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(100);
    }

    public async Task Refresh(ErrorAction onError)
    {
        await LoadManually(onError);
        await LoadCachedAssessmentDetails(onError);
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RefreshInBackground(CancellationToken token, ErrorAction onError)
    {
        while(!token.IsCancellationRequested)
        {
            await Task.Delay(RefreshInterval);
            await LoadManually(onError);
            await LoadCachedAssessmentDetails(onError, true);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
    }
}