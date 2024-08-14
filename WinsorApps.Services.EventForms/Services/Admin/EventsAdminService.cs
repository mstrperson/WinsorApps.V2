using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.EventForms;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using System.Collections.Immutable;
using System.Collections.Concurrent;
using WinsorApps.Services.Global;
using AsyncAwaitBestPractices;
using WinsorApps.Services.EventForms.Models.Admin;
using System.Text.Json;

namespace WinsorApps.Services.EventForms.Services.Admin;

public partial class EventsAdminService :
    IAsyncInitService,
    IAutoRefreshingCacheService
{
    private readonly EventFormsService _eventsService;
    private readonly ReadonlyCalendarService _calendar;
    private readonly RegistrarService _registrar;
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public event EventHandler? OnCacheRefreshed;

    public EventsAdminService(EventFormsService eventsService, ReadonlyCalendarService calendar, ApiService api, RegistrarService registrar, LocalLoggingService logging)
    {
        _eventsService = eventsService;
        _calendar = calendar;
        _api = api;
        _registrar = registrar;
        _logging = logging;
    }

    public ConcurrentDictionary<string, EventFormBase> AllEvents { get; protected set; } = [];

    public ImmutableArray<EventFormBase> PendingEvents => [.. AllEvents.Values.Where(evt => evt.status == ApprovalStatusLabel.Pending)];
    public ImmutableArray<EventFormBase> WaitingForRoom => [.. AllEvents.Values.Where(evt => evt.status == ApprovalStatusLabel.RoomNotCleared)];


    public bool Started { get; protected set; }

    public bool Ready { get; protected set; }

    public double Progress { get; protected set; }


    private TimeSpan _refInt = TimeSpan.FromMinutes(5);
    public TimeSpan RefreshInterval => _refInt;

    public bool Refreshing { get; protected set; }

    public async Task Initialize(ErrorAction onError)
    {
        await _api.WaitForInit(onError);
        await _registrar.WaitForInit(onError);
        Started = true;

        var schoolyear = _registrar.SchoolYears.First(sy => sy.startDate <= DateOnly.FromDateTime(DateTime.Today) && sy.endDate >= DateOnly.FromDateTime(DateTime.Today));
        var result = await GetAllEvents(onError, schoolyear.startDate.ToDateTime(default), schoolyear.endDate.ToDateTime(default));
        Progress = 0.5;
        AllEvents = new(result.Select(evt => new KeyValuePair<string, EventFormBase>(evt.id, evt)));

        Progress = 1;
        Ready = true;
        _lastUpdated = DateTime.Now;
    }

    private DateTime _lastUpdated;

    public async Task Refresh(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<EventFormBase>?>(HttpMethod.Get, $"api/events/admin/delta?since={_lastUpdated:yyyy-MM-dd HH:mm:ss}", onError: onError);
        if(result.HasValue && result.Value.Length > 0)
        {
            foreach(var evt in result.Value)
            {
                AllEvents.AddOrUpdate(evt.id, evt, (id, evt) => evt);
            }

            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        _lastUpdated = DateTime.Now;
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
            foreach (var evt in result.Value)
                AllEvents.AddOrUpdate(evt.id, evt, (id, e) => e);
        return result ?? [];
    }

    public async Task<ImmutableArray<EventFormBase>> GetTwoWeekListAsync(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<EventFormBase>?>(HttpMethod.Get, "api/events/admin/two-week-list", onError: onError);

        return result ?? [];
    }

    public async Task<ImmutableArray<EventFormBase>> GetAllEvents(ErrorAction onError, DateTime start, DateTime end)
    {
        var result = await _api.SendAsync<ImmutableArray<EventFormBase>?>(HttpMethod.Get,
            $"api/events/admin/all-events?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}", onError: onError);

        return result ?? [];
    }

    public async Task<OptionalStruct<EventFormBase>> ApproveEvent(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{eventId}/approve", onError: onError);
        if(result.HasValue)
        {
            AllEvents.AddOrUpdate(result.Value.id, result.Value, (id, e) => e);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
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
            AllEvents.AddOrUpdate(result.Value.id, result.Value, (id, e) => e);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{result.Value.summary} - {result.Value.start:yyyy-MM-dd} was declined.");
            return OptionalStruct<EventFormBase>.Some(result.Value);
        }

        return OptionalStruct<EventFormBase>.None();
    }

    public async Task SendNote(string eventId, CreateApprovalNote note, ErrorAction onError)
    {
        var evt = AllEvents[eventId];
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
