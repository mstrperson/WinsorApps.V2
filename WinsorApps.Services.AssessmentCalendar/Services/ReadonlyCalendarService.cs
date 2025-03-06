using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using AsyncAwaitBestPractices;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.AssessmentCalendar.Services;

public partial class ReadonlyCalendarService :
    IAsyncInitService,
    ICacheService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    public bool Ready { get; protected set; }
    public readonly CycleDayCollection CycleDays;

    public event EventHandler? FullYearCacheComplete;
    public event EventHandler? OnCacheRefreshed;

    public double Progress { get; protected set; } = 0;

    public string CacheFileName => ".assessment-calendar-ro.cache";

    public async Task SaveCache()
    {
        await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", JsonSerializer.Serialize(AssessmentCalendar));
    }

    public bool LoadCache()
    {
        if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
            return false;

        var cacheAge = DateTime.Now - File.GetCreationTime($"{_logging.AppStoragePath}{CacheFileName}");

        _logging.LogMessage(LocalLoggingService.LogLevel.Information,
            $"{CacheFileName} is {cacheAge.TotalDays:0.0} days old.");

        if(cacheAge.TotalDays > 14)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Deleting Aged Cache File.");
            File.Delete($"{_logging.AppStoragePath}{CacheFileName}");
            return false;
        }

        try
        {
            var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
            AssessmentCalendar = JsonSerializer.Deserialize<ImmutableArray<AssessmentCalendarEvent>>(json);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public ImmutableArray<AssessmentCalendarEvent> AssessmentCalendar { get; private set; } = [];

    public bool Started { get; private set; }

    public async Task MergeNewAssessments(IEnumerable<AssessmentCalendarEvent> result)
    {
        AssessmentCalendar = [.. AssessmentCalendar.ToList()
            .Merge(result, (a, b) => a.id == b.id)
            .OrderBy(evt => evt.start)];
        await SaveCache();
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
    }
    public ReadonlyCalendarService(ApiService api, LocalLoggingService logging, CycleDayCollection cycleDays)
    {
        _api = api;
        _logging = logging;
        this.CycleDays = cycleDays;
    }

    public async Task Initialize(ErrorAction onError)
    {
        int retryCount = 0;
        while (!await CycleDays.Initialize(onError) && retryCount++ < 5)
        {
            await Task.Delay(250);
        }

        if (!LoadCache())
        {
            AssessmentCalendar = await GetAssessmentsByMonth(DateTime.Today.Month, onError);
            

            Ready = true;
            var backgroundTask = Task.Run(async () =>
            {
                await RefreshYearCache(onError);
                retryCount = 0;
                while (AssessmentCalendar.Length == 0 && retryCount < 5)
                {
                    await Task.Delay(250);
                    await RefreshYearCache(onError);
                }
            });
            backgroundTask.WhenCompleted(() => FullYearCacheComplete?.Invoke(this, EventArgs.Empty));
            backgroundTask.SafeFireAndForget(e => e.LogException(_logging)); 
            await SaveCache();
        }
        else
        {
            RefreshYearCache(onError).SafeFireAndForget(e => e.LogException(_logging));
        }


        Ready = true;
    }

    public async Task<ImmutableArray<AssessmentEntryRecord>> GetAssessmentsFor(string sectionId, ErrorAction onError)
    {
        var result = await _api.GetPagedResult<AssessmentEntryRecord>(
            HttpMethod.Get,
            $"api/assessment-calendar/section/{sectionId}", onError: onError);

        return result;
    }

    private async Task RefreshYearCache(ErrorAction onError)
    {
        Progress = 0;
        var calendarCache = new List<AssessmentCalendarEvent>();

        var totalTime = CycleDays.SchoolYear.endDate.ToDateTime(default) - CycleDays.SchoolYear.startDate.ToDateTime(default);
        for (var swp = new SixWeekPeriod() { StartDate = CycleDays.SchoolYear.startDate }; swp.StartDate < CycleDays.SchoolYear.endDate; swp = swp.Next)
        {
            var temp = await GetAssessmentCalendarInRange(onError, swp.StartDate, swp.EndDate);
            calendarCache.AddRange(temp);
            var progressTime = swp.EndDate.ToDateTime(default) - CycleDays.SchoolYear.startDate.ToDateTime(default);
            Progress = progressTime.Ticks / totalTime.Ticks;
        }

        AssessmentCalendar = [.. calendarCache];
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetAssessmentsByMonth(Month month, ErrorAction onError)
    {
        DateRange monthRange = DateRange.MonthOf(month, CycleDays.SchoolYear.startDate);
        var result = await GetAssessmentCalendarInRange(onError, monthRange.start, monthRange.end);
        AssessmentCalendar = [.. AssessmentCalendar.ToList()
            .Merge(result, (a, b) => a.id == b.id)
            .OrderBy(evt => evt.start)];
        return result;
    }

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetAssessmentCalendarOn(DateOnly date, ErrorAction onError)
    {
        var result = await _api.GetPagedResult<AssessmentCalendarEvent>(
            HttpMethod.Get,
            $"api/assessment-calendar?date={date:yyyy-MM-dd}", onError: onError);

        AssessmentCalendar = [.. AssessmentCalendar.ToList()
            .Merge(result, (a, b) => a.id == b.id)
            .OrderBy(evt => evt.start)];
        return result;
    }

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetAssessmentCalendarInRange(ErrorAction onError, DateOnly start = default, DateOnly end = default)
    {
        if (start == default) { start = DateOnly.FromDateTime(DateTime.Today); }
        var param = end == default ? "" : $"&end={end:yyyy-MM-dd}";
        var result = await _api.GetPagedResult<AssessmentCalendarEvent>(
            HttpMethod.Get,
            $"api/assessment-calendar?start={start:yyyy-MM-dd}{param}",
            onError: onError);

        AssessmentCalendar = [.. AssessmentCalendar.ToList()
            .Merge(result, (a, b) => a.id == b.id)
            .OrderBy(evt => evt.start)];
        return result;
    }

    public async Task<ImmutableArray<AssessmentGroup>> GetAssessmentGroups(ErrorAction onError, DateOnly start = default, DateOnly end = default)
    {
        char ch = '?';
        var query = "";
        if (start != default)
        {
            query = $"?start={start:yyyy-MM-dd}";
            ch = '&';
        }
        if (end != default)
        {
            query += $"{ch}end={end:yyyy-MM-dd}";
        }

        return await _api.GetPagedResult<AssessmentGroup>(
            HttpMethod.Get, 
            $"api/assessment-calendar/assessments{query}",
            onError: onError);
    }

    public async Task<AssessmentEntryShort?> GetAssessmentGroup(string assessmentId, ErrorAction onError) =>
        await _api.SendAsync<AssessmentEntryShort?>(HttpMethod.Get, $"api/assessment-calendar/assessments/{assessmentId}",
            onError: onError);

    public async Task<APExamDetail?> GetAPExamDetails(string examId, ErrorAction onError) =>
        await _api.SendAsync<APExamDetail?>(HttpMethod.Get, $"api/assessment-calendar/ap-exam/{examId}",
            onError: onError);

    public async Task<DayNote?> GetDayNote(string noteId, ErrorAction onError) =>
        await _api.SendAsync<DayNote?>(HttpMethod.Get, $"api/assessment-calendar/note/{noteId}",
            onError: onError);

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }

    public async Task Refresh(ErrorAction onError)
    {
        await RefreshYearCache(onError);
    }
}

public class CycleDayCollection :
    IEnumerable<CycleDay>,
    IAsyncInitService,
    ICacheService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public CycleDayCollection(ApiService api, LocalLoggingService logging)
    {
        _api = api;
        _logging = logging;

    }

    private ImmutableArray<CycleDay> _cycleDays = [];

    public event EventHandler? OnCacheRefreshed;

    private readonly record struct CacheStructure(SchoolYear schoolYear, ImmutableArray<CycleDay> cycleDays);
    public string CacheFileName => ".cycle-days.cache";

    public async Task SaveCache()
    {
        var cache = new CacheStructure(SchoolYear, _cycleDays);
        await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", JsonSerializer.Serialize(cache));
    }

    public bool LoadCache()
    {
        if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
            return false;

        if (File.GetCreationTime($"{_logging.AppStoragePath}{CacheFileName}").OlderThan(TimeSpan.FromDays(14)))
        {
            File.Delete($"{_logging.AppStoragePath}{CacheFileName}");
            return false;
        }

        try
        {
            var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
            var cache = JsonSerializer.Deserialize<CacheStructure>(json);

            if (cache.schoolYear.endDate.ToDateTime(default) < DateTime.Today)
                return false;

            SchoolYear = cache.schoolYear;
            _cycleDays = cache.cycleDays;
            CacheStartDate = _cycleDays.Select(cd => cd.date).Min();
            CacheEndDate = _cycleDays.Select(cd => cd.date).Max();
        }
        catch
        {
            return false;
        }

        return true;
    }
    public DateOnly CacheStartDate { get; private set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly CacheEndDate { get; private set; } = DateOnly.FromDateTime(DateTime.Today);

    public bool Ready { get; private set; } = false;

    public SchoolYear SchoolYear { get; private set; }

    public bool Started { get; protected set; }

    public double Progress { get; private set; }

    public CycleDay? this[DateOnly date]
    {
        get
        {
            if (!Ready) throw new ServiceNotReadyException(_logging);

            if (!_cycleDays.Any(cd => cd.date == date)) return null;

            return _cycleDays.First(cd => cd.date == date);
        }
    }

    public CycleDay? this[DateTime date] => this[DateOnly.FromDateTime(date)];

    public ImmutableArray<DateOnly> this[string cycleDay]
    {
        get
        {
            if (!Ready) throw new ServiceNotReadyException(_logging);

            return _cycleDays
                .Where(cd => cd.cycleDay == cycleDay)
                .Select(cd => cd.date)
                .ToImmutableArray();
        }
    }



    public async Task<bool> Initialize(ErrorAction onError)
    {
        Started = true;
        if (!LoadCache())
        {
            var schoolYear = await _api.SendAsync<SchoolYear?>(HttpMethod.Get, "api/schedule/school-year",
                    onError: onError);


            if (!schoolYear.HasValue)
            {
                _cycleDays = [];
                Ready = true;
                return false;
            }

            SchoolYear = schoolYear.Value;

            _ = await GetCycleDays(schoolYear.Value.startDate, schoolYear.Value.endDate, onError);
            await SaveCache();
        }
        Ready = true;
        Progress = 1;
        return true;
    }

    public IEnumerator<CycleDay> GetEnumerator()
    {
        return ((IEnumerable<CycleDay>)_cycleDays).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_cycleDays).GetEnumerator();
    }


    public async Task<ImmutableArray<CycleDay>> GetCycleDays(DateOnly start, DateOnly end, ErrorAction onError)
    {
        if (start < CacheStartDate || end > CacheEndDate)
        {
            if (end < CacheStartDate)
                end = CacheStartDate;

            var result = await _api.SendAsync<ImmutableArray<CycleDay>>(HttpMethod.Get, $"api/schedule/cycle-day?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}",
                onError: onError);

            _cycleDays = _cycleDays.Merge(result, (a, b) => a.date == b.date);

            if (start < CacheStartDate)
                CacheStartDate = start;
            if(end > CacheEndDate)
                CacheEndDate = end;
        }

        return [.._cycleDays.Where(cd => cd.date >= start && cd.date <= end)];
    }

    async Task IAsyncInitService.Initialize(ErrorAction onError)
    {
        await this.Initialize(onError);
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(100);
    }

    public async Task Refresh(ErrorAction onError) => _ = await Initialize(onError);
}