using System.Collections;
using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.AssessmentCalendar.Services;

public class ReadonlyCalendarService :
    IAsyncInitService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    public bool Ready { get; protected set; }
    public readonly CycleDayCollection CycleDays;

    public event EventHandler? FullYearCacheComplete;

    public double Progress { get; protected set; } = 0;

    public ImmutableArray<AssessmentCalendarEvent> AssessmentCalendar { get; private set; } = [];

    public bool Started { get; private set; }

    public void MergeNewAssessments(IEnumerable<AssessmentCalendarEvent> result)
    {
        AssessmentCalendar = AssessmentCalendar.ToList()
            .Merge(result, (a, b) => a.id == b.id)
            .OrderBy(evt => evt.start)
            .ToImmutableArray();
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

        AssessmentCalendar = calendarCache.ToImmutableArray();
    }

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetAssessmentsByMonth(Month month, ErrorAction onError)
    {
        DateRange monthRange = DateRange.MonthOf(month, CycleDays.SchoolYear.startDate);
        var result = await GetAssessmentCalendarInRange(onError, monthRange.start, monthRange.end);
        AssessmentCalendar = AssessmentCalendar.ToList()
            .Merge(result, (a, b) => a.id == b.id)
            .OrderBy(evt => evt.start)
            .ToImmutableArray();
        return result;
    }

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetAssessmentCalendarOn(DateOnly date, ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get,
            $"api/assessment-calendar?date={date:yyyy-MM-dd}", onError: onError);

        AssessmentCalendar = AssessmentCalendar.ToList()
            .Merge(result, (a, b) => a.id == b.id)
            .OrderBy(evt => evt.start)
            .ToImmutableArray();
        return result;
    }

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetAssessmentCalendarInRange(ErrorAction onError, DateOnly start = default, DateOnly end = default)
    {
        if (start == default) { start = DateOnly.FromDateTime(DateTime.Today); }
        var param = end == default ? "" : $"&end={end:yyyy-MM-dd}";
        var result = await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get,
            $"api/assessment-calendar?start={start:yyyy-MM-dd}{param}",
            onError: onError);

        AssessmentCalendar = AssessmentCalendar.ToList()
            .Merge(result, (a, b) => a.id == b.id)
            .OrderBy(evt => evt.start)
            .ToImmutableArray();
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

        return await _api.SendAsync<ImmutableArray<AssessmentGroup>>(HttpMethod.Get, $"api/assessment-calendar/assessments{query}",
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

    public Task WaitForInit(ErrorAction onError)
    {
        throw new NotImplementedException();
    }

    public Task Refresh(ErrorAction onError)
    {
        throw new NotImplementedException();
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

        var schoolYear = await _api.SendAsync<SchoolYear?>(HttpMethod.Get, "api/schedule/school-year",
                onError: onError);

        Progress = 1;

        if (!schoolYear.HasValue)
        {
            _cycleDays = Array.Empty<CycleDay>().ToImmutableArray();
            Ready = true;
            return false;
        }

        SchoolYear = schoolYear.Value;

        _ = await GetCycleDays(schoolYear.Value.startDate, schoolYear.Value.endDate, onError);
        Ready = true;
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