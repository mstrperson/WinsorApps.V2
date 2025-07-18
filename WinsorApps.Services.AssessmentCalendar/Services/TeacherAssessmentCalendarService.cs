using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using AsyncAwaitBestPractices;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.AssessmentCalendar.Services;

public partial class TeacherAssessmentService(ApiService api, LocalLoggingService logging, ReadonlyCalendarService calendar) : 
    IAsyncInitService,
    IAutoRefreshingCacheService
{
    private readonly ApiService _api = api;
    private readonly LocalLoggingService _logging = logging;
    private readonly ReadonlyCalendarService _calendar = calendar;

    public List<StudentRecordShort> MyStudents { get; private set; } = [];
    public List<CourseRecord> CourseList { get; private set; } = [];


    public event EventHandler? OnCacheRefreshed;

    public List<AssessmentGroup> MyAssessments { get; private set; } = [];

    public string CacheFileName => ".teacher-assessment-calendar.cache";
    public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }

    private record CacheStructure(
        List<StudentRecordShort> students,
        List<CourseRecord> courses,
        List<AssessmentGroup> assessments,
        List<StudentLateWorkCollection> lateWork,
        Dictionary<string, AssessmentEntryRecord> detailsCache);

    public async Task SaveCache()
    {
        var retryCount = 0;
        TryAgain:
         var cache = new CacheStructure(MyStudents, CourseList, [..MyAssessments], _adviseeLateWork, AssessmentDetailsCache);
        try
        {
            await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", JsonSerializer.Serialize(cache));
        }
        catch(Exception e)
        {
            e.LogException(_logging);
            if(retryCount < 5)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(15));
                goto TryAgain;
            }
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
            if(cache is null) return false;

            MyStudents = cache.students;
            CourseList = cache.courses;
            MyAssessments = [.. cache.assessments];
            _adviseeLateWork = cache.lateWork;
            AssessmentDetailsCache = cache.detailsCache ?? [];
        }
        catch
        {
            return false;
        }

        return CourseList.Count > 0;
    }

    public bool Ready { get; private set; } = false;

    public bool Started { get; private set; }

    public double Progress { get; private set; }

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
            .Where(ent => ent.type == AssessmentType.Assessment && ent.start >= DateTime.Today.MonthOf())
            .Select(ent => ent.id)
            .ToList();

        using DebugTimer __ = new($"Loading Cached assessment details for {assessmentIds.Count} assessments", _logging);
        _ = await GetAssessmentDetails(assessmentIds, onError, reloadCache);
    }

    private async Task LoadManually(ErrorAction onError)
    {
        var studentTask = GetMyStudentList(onError);
        studentTask.WhenCompleted(() =>
        {
            MyStudents = studentTask.Result;
            Progress += 0.25;
        },
        () =>
        {
            Progress += 0.25;
        });
        var courseTask = GetMyCourseList(onError);
        courseTask.WhenCompleted(() =>
        {
            CourseList = courseTask.Result;
            Progress += 0.25;
        },
        () =>
        {
            Progress += 0.25;
        });
        var assessmentTask = GetMyAssessments(onError, DateTime.Today, DateTime.Today.AddYears(1));
        assessmentTask.WhenCompleted(() =>
        {
            MyAssessments = [.. assessmentTask.Result];
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
        await SaveCache();
    }

    public async Task<List<AssessmentEntryRecord>> GetAssessmentsFor(ErrorAction onError, string sectionId, DateTime start = default, DateTime end = default)
    {
        var query = "";
        if (start != default)
        {
            query += $"?start={start:yyyy-MM-dd}";
            if (end != default)
                query += $"&end={end:yyyy-MM-dd}";
        }
        var result = await _api.SendAsync<List<AssessmentEntryRecord>>(HttpMethod.Get,
            $"api/assessment-calendar/teachers/sections/{sectionId}{query}",
            onError: onError) ?? [];

        return result;
    }


    public async Task<List<AssessmentGroup>> GetMyAssessments(ErrorAction onError, DateTime start = default, DateTime end = default)
    {
        var query = "";
        if (start == default)
        {
            var res = await _api.GetPagedResult<AssessmentGroup>(
                HttpMethod.Get,
                "api/assessment-calendar/teachers",
                onError: onError);

            if (res.ExceptBy(MyAssessments.Select(grp => grp.id), grp => grp.id).Any())
            {
                MyAssessments = MyAssessments.Merge(res, (oldgrp, newgrp) => oldgrp.id == newgrp.id);
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }

            return res;
        }
        query += $"?start={start:yyyy-MM-dd}";
        if (end != default)
            query += $"&end={end:yyyy-MM-dd}";

        var result = await _api.SendAsync<List<AssessmentGroup>>(
            HttpMethod.Get,
            $"api/assessment-calendar/teachers{query}",
            onError: onError);

        if (result?.ExceptBy(MyAssessments.Select(grp => grp.id), grp => grp.id).Any() ?? false)
        {
            MyAssessments = MyAssessments.Merge(result, (oldgrp, newgrp) => oldgrp.id == newgrp.id);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        return result ?? [];
    }

    public async Task<List<AssessmentCalendarEvent>> GetCalendarByClassOn(string[] classes, DateTime date, ErrorAction onError)
    {
        var result = await _api.GetPagedResult<string[], AssessmentCalendarEvent>(
            HttpMethod.Post,
            $"api/assessment-calendar/by-class?date={date:yyyy-MM-dd}",
            classes,
            onError: onError);

        await _calendar.MergeNewAssessments(result);
        return result;
    }
    public async Task<List<AssessmentCalendarEvent>> GetCalendarByClassInRange(ErrorAction onError, string[] classes, DateTime start = default, DateTime end = default)
    {
        if (start == default) { start = DateTime.Today; }
        var param = end == default ? "" : $"&end={end:yyyy-MM-dd}";
        var result = await _api.GetPagedResult<string[], AssessmentCalendarEvent>(
            HttpMethod.Post,
            $"api/assessment-calendar/by-class?start={start:yyyy-MM-dd}{param}",
            classes,
            onError: onError);
        await _calendar.MergeNewAssessments(result);
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

        if(result is not null)
        {
            if (!AssessmentDetailsCache.TryAdd(assessmentId, result))
                AssessmentDetailsCache[assessmentId] = result;
        }
        await SaveCache();
        return result;
    }

    public async Task<List<AssessmentEntryRecord>> GetAssessmentDetails(List<string> assessmentIds, ErrorAction onError, bool refresheCache = false)
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

        var result = await _api.SendAsync<List<string>, List<AssessmentEntryRecord>>(
                HttpMethod.Post,
            $"api/assessment-calendar/teachers/assessment-details", assessmentIds,
            onError: onError);

        output = [.. output, .. result];
        foreach(var entry in result)
        {
            AssessmentDetailsCache.AddOrUpdate(entry.assessmentId, entry);
        }

        await SaveCache();
        return [..output];
    }

    public async Task<List<CourseRecord>> GetMyCourseList(ErrorAction onError) =>
        await _api.SendAsync<List<CourseRecord>>(HttpMethod.Get,
            "api/assessment-calendar/teachers/course-list",
            onError: onError) ?? [];

    public async Task<List<SectionMinimalRecord>> GetMySectionsOf(string courseId, ErrorAction onError) =>
                await _api.SendAsync<List<SectionMinimalRecord>>(HttpMethod.Get,
                    $"api/assessment-calendar/teachers/courses/{courseId}/sections",
                    onError: onError) ?? [];
    public async Task<AssessmentGroup?> GetMyCourseList(string assessmentId, ErrorAction onError) =>
        await _api.SendAsync<AssessmentGroup?>(HttpMethod.Get, $"api/assessment-calendar/teachers/{assessmentId}",
            onError: onError);

    public async Task<List<AssessmentCalendarEvent>> GetMyCalendarInRange(ErrorAction onError, DateTime start = default, DateTime end = default)
    {
        if (start == default) { start = DateTime.Today; }
        var param = end == default ? "" : $"&end={end:yyyy-MM-dd}";
        var result = await _api.GetPagedResult<AssessmentCalendarEvent>(HttpMethod.Get,
            $"api/assessment-calendar/teachers?start={start:yyyy-MM-dd}{param}",
            onError: onError);
        await _calendar.MergeNewAssessments(result);
        return result;
    }

    public async Task<AssessmentGroup?> CreateNewAssessment(CreateAssessmentRecord newAssessment, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateAssessmentRecord, AssessmentGroup?>(
            HttpMethod.Post, $"api/assessment-calendar/teachers", newAssessment,
            onError: onError);

        if (result is null) 
            return result;

        MyAssessments.Add(result);
        // TODO: This is awful. stop.
        _calendar.Refresh(onError).SafeFireAndForget(e => e.LogException(_logging));
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        await SaveCache();
        return result;
    }
    public async Task<AssessmentGroup?> UpdateAssessment(string groupId, CreateAssessmentRecord newAssessment, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateAssessmentRecord, AssessmentGroup?>(
            HttpMethod.Put, $"api/assessment-calendar/teachers/{groupId}", newAssessment,
            onError: onError);

        if (result is null) return result;

        var group = MyAssessments.FirstOrDefault(g => g.id == groupId);
        if (group != default)
            MyAssessments.Replace(group, result);
        else
            MyAssessments.Add(result);

        // TODO: This is awful!! STOP
        _calendar.Refresh(onError).SafeFireAndForget(e => e.LogException(_logging));
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        await SaveCache();
        return result;
    }

    public async Task DeleteAssessment(string groupId, ErrorAction onError)
    {
        var error = false;
        await _api.SendAsync(HttpMethod.Delete, $"api/assessment-calendar/teachers/{groupId}", onError: onError);

        if (!error)
        {
            var group = MyAssessments.FirstOrDefault(g => g.id == groupId);
            if (group != default)
                MyAssessments.Remove(group);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            await SaveCache();
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