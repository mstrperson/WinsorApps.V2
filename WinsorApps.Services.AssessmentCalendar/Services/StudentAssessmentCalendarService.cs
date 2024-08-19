using AsyncAwaitBestPractices;
using System.Collections.Immutable;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.AssessmentCalendar.Services;


public partial class StudentAssessmentService :
    IAsyncInitService,
    ICacheService

{

    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public bool Started { get; private set; }

    public bool Ready { get; private set; }

    public double Progress { get; private set; }

    public StudentAssessmentService(ApiService api, LocalLoggingService logging)
    {
        _api = api;
        _logging = logging;
    }

    public DateTime CacheStartDate = DateTime.Today;
    public DateTime CacheEndDate = DateTime.Today;

    public event EventHandler? OnCacheRefreshed;

    public ImmutableArray<AssessmentCalendarEvent> MyCalendar { get; private set; } = [];

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetMyCalendarOn(DateTime date, ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>?>(HttpMethod.Get, 
            $"api/assessment-calendar/students?date", onError: onError) ?? [];

        _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Got My Calendar On {date:yyyy-mm-dd}", $"{result.Length} items returned");
        if (result.Any())
        {
            MyCalendar = [.. MyCalendar.Merge(result, (a, b) => a.id == b.id && a.type == b.type)];
            OnCacheRefreshed?.Invoke(this, StudentAssessmentCacheRefreshedEventArgs.CalendarRefreshed);
        }

        return result;
    }

    public async Task<AssessmentEntryShort?> GetAssessmentDetails(string assessmentId, ErrorAction onError)
    {
        var result = await _api.SendAsync<AssessmentEntryShort?>(HttpMethod.Get, $"api/assessment-calendar/assessment/{assessmentId}", onError: onError);

        return result;
    }
    public async Task<APExamDetail?> GetApExamDetails(string examId, ErrorAction onError)
    {
        var result = await _api.SendAsync<APExamDetail?>(HttpMethod.Get, $"api/assessment-calendar/assessment/{examId}", onError: onError);

        return result;
    }

    public async Task<ImmutableArray<AssessmentCalendarEvent>> GetMyCalendarInRange(ErrorAction onError, DateTime start = default, DateTime end = default)
    {
        if (start == default) 
        { 
            start = DateTime.Today; 
        }

        if (start < CacheStartDate && end < CacheEndDate)
            end = CacheStartDate;

        var param = end == default ? "" : $"&toDate={end:yyyy-MM-dd}";
        
        var result = await _api.SendAsync<ImmutableArray<AssessmentCalendarEvent>>(HttpMethod.Get,
            $"api/assessment-calendar/students?fromDate={start:yyyy-MM-dd}{param}",
            onError: onError);

        MyCalendar = [.. MyCalendar.Merge(result, (a, b) => a.id == b.id && a.type == b.type)];

        if (start < CacheStartDate)
            CacheStartDate = start;

        if (end > CacheEndDate)
            CacheEndDate = end;

        if (result.Any())
            OnCacheRefreshed?.Invoke(this, StudentAssessmentCacheRefreshedEventArgs.CalendarRefreshed);

        return result;
    }

    public ImmutableArray<AssessmentPassDetail> MyLatePasses { get; private set; } = [];

    public async Task<ImmutableArray<AssessmentPassDetail>> GetMyPasses(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<AssessmentPassDetail>>(HttpMethod.Get, "api/assessment-calendar/students/passes?showPast=true", onError: onError);
        MyLatePasses = [.. MyLatePasses.Merge(result, (a, b) => a.assessment.id == b.assessment.id && a.assessment.type == b.assessment.type)];
        if(result.Any())
            OnCacheRefreshed?.Invoke(this, StudentAssessmentCacheRefreshedEventArgs.PassesRefreshed);
        return result;
    }
    public async Task<bool> WithdrawLatePass(string assessmentId, ErrorAction onError)
    {
        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/assessment-calendar/students/passes/{assessmentId}",
            onError: err => 
            {
                success = false;
                onError(err);
            });
        
        if(success)
        {
            MyLatePasses = MyLatePasses.Remove(MyLatePasses.FirstOrDefault(p => p.assessment.id == assessmentId));
            var oldAssessment = MyCalendar.First(evt => evt.type == AssessmentType.Assessment && evt.id == assessmentId);
            MyCalendar = MyCalendar.Replace(oldAssessment, oldAssessment with { passAvailable = true, passUsed = false });
            OnCacheRefreshed?.Invoke(this, StudentAssessmentCacheRefreshedEventArgs.PassesRefreshed);
        }

        return success;
    }

    public async Task<AssessmentPass?> RequestLatePass(string assessmentId, ErrorAction onError)
    {
        var result = await _api.SendAsync<AssessmentPass?>(HttpMethod.Post, $"api/assessment-calendar/students/passes/{assessmentId}",
            onError: onError);

        if (result.HasValue)
        {
            await GetMyPasses(onError);
            var oldAssessment = MyCalendar.First(evt => evt.type == AssessmentType.Assessment && evt.id == assessmentId);
            MyCalendar = MyCalendar.Replace(oldAssessment, oldAssessment with { passAvailable = false, passUsed = true });
            OnCacheRefreshed?.Invoke(this, StudentAssessmentCacheRefreshedEventArgs.PassesRefreshed);
        }

        return result;
    }

    public async Task Initialize(ErrorAction onError)
    {
        await _api.WaitForInit(onError);

        Started = true;

        var myPasses = GetMyPasses(onError);
        myPasses.WhenCompleted(() => Progress += 0.5);
        myPasses.SafeFireAndForget(e => e.LogException(_logging));


        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var myCalendar = GetMyCalendarInRange(onError, thisMonth, thisMonth.AddMonths(1));
        myCalendar.WhenCompleted(() => Progress += 0.5);
        myCalendar.SafeFireAndForget(e => e.LogException(_logging));

        await Task.WhenAll(myPasses, myCalendar);

        Progress = 1;
        Ready = true;
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(100);
    }

    public async Task Refresh(ErrorAction onError)
    {
        MyCalendar = [];

        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        CacheStartDate = thisMonth;
        CacheEndDate = thisMonth.AddMonths(1);
        await GetMyCalendarInRange(onError, thisMonth, thisMonth.AddMonths(1));
    }
}

public class StudentAssessmentCacheRefreshedEventArgs : EventArgs
{
    public static StudentAssessmentCacheRefreshedEventArgs CalendarRefreshed => new() { Calendar = true, Passes = false };
    public static StudentAssessmentCacheRefreshedEventArgs PassesRefreshed => new() { Calendar = false, Passes = true };

    private StudentAssessmentCacheRefreshedEventArgs() { }

    public required bool Calendar { get; init; }
    public required bool Passes { get; init; }
}