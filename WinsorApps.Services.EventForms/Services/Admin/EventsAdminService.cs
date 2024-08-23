using WinsorApps.Services.Global.Services;
using WinsorApps.Services.EventForms.Models;
using System.Collections.Immutable;
using WinsorApps.Services.Global;
using WinsorApps.Services.EventForms.Models.Admin;
using System.Text.Json;
using System.Diagnostics;
using AsyncAwaitBestPractices;

namespace WinsorApps.Services.EventForms.Services.Admin;

public partial class EventsAdminService :
    IAsyncInitService,
    IAutoRefreshingCacheService
{
    private readonly RegistrarService _registrar;
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public event EventHandler? OnCacheRefreshed;

    public EventsAdminService(ApiService api, RegistrarService registrar, LocalLoggingService logging)
    {
        _api = api;
        _registrar = registrar;
        _logging = logging;
    }

    public DateTime CacheStartDate { get; private set; }
    public DateTime CacheEndDate { get; private set; }

    public ImmutableArray<EventFormBase> AllEvents { get; protected set; } = [];

    public ImmutableArray<EventFormBase> PendingEvents => [.. AllEvents.Where(evt => evt.status == ApprovalStatusLabel.Pending)];
    public ImmutableArray<EventFormBase> WaitingForRoom => [.. AllEvents.Where(evt => evt.status == ApprovalStatusLabel.RoomNotCleared)];


    public bool Started { get; protected set; }

    public bool Ready { get; protected set; }

    public double Progress { get; protected set; }


    private TimeSpan _refInt = TimeSpan.FromMinutes(5);
    public TimeSpan RefreshInterval => _refInt;

    public bool Refreshing { get; protected set; }

    public async Task Initialize(ErrorAction onError)
    {
        await _api.WaitForInit(onError);
        while (!_registrar.SchoolYears.Any())
            await Task.Delay(100);
        Started = true;

        var start = DateTime.Today.MonthOf().MondayOf();
        var end = start.AddMonths(1).MondayOf().AddDays(6);
        CacheStartDate = DateTime.Today;
        CacheEndDate = DateTime.Today;
        _ = await GetAllEvents(onError, start, end);
        Progress = 0.33;
        _ = await GetPendingEventsAsync(onError);
        Progress = 0.66;
        _ = await GetRoomPendingEvents(onError);
        Progress = 1;
        Ready = true;
        _lastUpdated = DateTime.Now;
        
        LoadInBackground().SafeFireAndForget(e => e.LogException(_logging));
    }

    private async Task LoadInBackground()
    {
        var schoolYear =
            _registrar.SchoolYears.First(sy => sy.startDate <= DateOnly.FromDateTime(DateTime.Today) && sy.endDate >= DateOnly.FromDateTime(DateTime.Today));

        for (SixWeekPeriod swp = new() {StartDate = DateOnly.FromDateTime(CacheEndDate)};
             swp.StartDate < schoolYear.endDate;
             swp = swp.Next)
        {
            _ = await GetAllEvents(_logging.LogError, swp.StartDate.ToDateTime(default), swp.EndDate.ToDateTime(default), true);
        }
        
        _ = await GetAllEvents(_logging.LogError, schoolYear.startDate.ToDateTime(default), CacheStartDate, true);
    }

    private DateTime _lastUpdated;

    public async Task Refresh(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<EventFormBase>?>(HttpMethod.Get, $"api/events/admin/delta?since={_lastUpdated:yyyy-MM-dd HH:mm:ss}", onError: onError);
        if(result.HasValue && result.Value.Length > 0)
        {
            ComputeChangesAndUpdates(result.Value);   
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        _lastUpdated = DateTime.Now;
    }

    private void ComputeChangesAndUpdates(ImmutableArray<EventFormBase> incoming, bool suppressRefresh = false)
    {
        using DebugTimer _ = new("Computing Changes and Updates", _logging);
        var newEvents = incoming.Where(evt => AllEvents.All(existing => existing.id != evt.id)).ToImmutableArray();
        Debug.WriteLine($"{newEvents.Length} are not cached events.");

        var changes = incoming.Except(newEvents).Where(evt => AllEvents.All(existing => !existing.IsSameAs(evt))).ToImmutableArray();
        Debug.WriteLine($"{changes.Length} Events that are different.");

        var toReplace = AllEvents.Where(evt => changes.Any(update => update.id == evt.id)).ToImmutableArray();

        AllEvents = [.. AllEvents.Except(toReplace).Union(changes).Union(newEvents)];
        if(!suppressRefresh)
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(100);
    }

    public async Task<ImmutableArray<EventFormBase>> GetPendingEventsAsync(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<EventFormBase>?>(HttpMethod.Get, "api/events/admin/pending-events", onError: onError);
        if(result.HasValue)
        {
            ComputeChangesAndUpdates(result.Value);
        }
        return result ?? [];
    }

    public async Task<ImmutableArray<EventFormBase>> GetTwoWeekListAsync(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<EventFormBase>?>(HttpMethod.Get, "api/events/admin/two-week-list", onError: onError);

        return result ?? [];
    }

    public async Task<ImmutableArray<EventFormBase>> GetAllEvents(ErrorAction onError, DateTime start, DateTime end, bool suppressRefresh = false)
    {
        if (end < CacheStartDate)
            end = CacheStartDate;

        if(start > CacheEndDate)
            start = CacheEndDate;

        if (CacheStartDate > start)
            CacheStartDate = start;

        var result = await _api.SendAsync<ImmutableArray<EventFormBase>?>(HttpMethod.Get,
            $"api/events/admin/all-events?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}", onError: onError);

        if (result.HasValue)
        {
            ComputeChangesAndUpdates(result.Value);
        }

        return result ?? [];
    }

    public async Task<OptionalStruct<EventFormBase>> ApproveEvent(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{eventId}/approve", onError: onError);
        if(result.HasValue)
        {
            ComputeChangesAndUpdates([result.Value]);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{result.Value.summary} - {result.Value.start:yyyy-MM-dd} was approved.");
            return OptionalStruct<EventFormBase>.Some(result.Value);
        }

        return OptionalStruct<EventFormBase>.None();
    }
    public async Task<OptionalStruct<EventFormBase>> DeclineEvent(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{eventId}/decline", onError: onError);
        if (result.HasValue)
        {
            ComputeChangesAndUpdates([result.Value]);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{result.Value.summary} - {result.Value.start:yyyy-MM-dd} was declined.");
            return OptionalStruct<EventFormBase>.Some(result.Value);
        }

        return OptionalStruct<EventFormBase>.None();
    }

    public async Task SendNote(string eventId, CreateApprovalNote note, ErrorAction onError)
    {
        var evt = AllEvents.FirstOrDefault(evt=> evt.id == eventId);
        await _api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/approve", JsonSerializer.Serialize(note), onError: onError);
        _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Note submitted to event {evt.summary} - {evt.start:yyyy-MM-dd}.");
    }

    public async Task<ImmutableArray<EventApprovalStatusRecord>> GetHistory(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<EventApprovalStatusRecord>?>(HttpMethod.Get,
            $"api/events/{eventId}/approval-history", onError: onError);

        return result ?? [];
    }

    public async Task RefreshInBackground(CancellationToken token, ErrorAction onError)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(RefreshInterval);
            await Refresh(onError);
        }
    }
}
