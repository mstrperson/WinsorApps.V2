using WinsorApps.Services.Global.Services;
using WinsorApps.Services.EventForms.Models;
using System.Collections.Immutable;
using WinsorApps.Services.Global;
using WinsorApps.Services.EventForms.Models.Admin;
using System.Text.Json;
using System.Diagnostics;
using AsyncAwaitBestPractices;

namespace WinsorApps.Services.EventForms.Services.Admin;

public partial class EventsAdminService(ApiService api, RegistrarService registrar, LocalLoggingService logging) :
    IAsyncInitService,
    IAutoRefreshingCacheService
{
    private readonly RegistrarService _registrar = registrar;
    private readonly ApiService _api = api;
    private readonly LocalLoggingService _logging = logging;

    public event EventHandler? OnCacheRefreshed;

    public DateTime CacheStartDate { get; private set; }
    public DateTime CacheEndDate { get; private set; }

    public List<EventFormBase> AllEvents { get; protected set; } = [];

    public List<EventFormBase> PendingEvents => [.. AllEvents.Where(evt => evt.status == ApprovalStatusLabel.Pending)];
    public List<EventFormBase> WaitingForRoom => [.. AllEvents.Where(evt => evt.status == ApprovalStatusLabel.RoomNotCleared)];
    public string CacheFileName => ".events-admin.cache";
    public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }
    public async Task SaveCache()
    {
        var json = JsonSerializer.Serialize(AllEvents);
        await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", json);
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
            _lastUpdated = File.GetLastWriteTime($"{_logging.AppStoragePath}{CacheFileName}");
            var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
            AllEvents = JsonSerializer.Deserialize<List<EventFormBase>>(json) ?? [];
            CacheStartDate = AllEvents.Select(evt => evt.start).Min();
            CacheEndDate = AllEvents.Select(evt => evt.start).Max();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Started { get; protected set; }

    public bool Ready { get; protected set; }

    public double Progress { get; protected set; }


    private readonly TimeSpan _refInt = TimeSpan.FromMinutes(5);
    public TimeSpan RefreshInterval => _refInt;

    public bool Refreshing { get; protected set; }

    public async Task Initialize(ErrorAction onError)
    {
        await _api.WaitForInit(onError);
        while (_registrar.SchoolYears.Count == 0)
            await Task.Delay(100);
        Started = true;

        if (!LoadCache())
        {
            var start = DateTime.Today.MonthOf().MondayOf();
            var end = start.AddMonths(1).MondayOf().AddDays(6);
            CacheStartDate = DateTime.Today;
            CacheEndDate = DateTime.Today;
            _ = await GetAllEvents(onError, start, end);
            Progress = 0.33;
            _ = await GetPendingEventsAsync(onError);
            Progress = 0.66;
            _ = await GetRoomPendingEvents(onError);
            await SaveCache();
        }
        else
        {
            Refresh(onError).SafeFireAndForget(e => e.LogException(_logging));
        }
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

        await SaveCache();
    }

    private DateTime _lastUpdated;

    public async Task Refresh(ErrorAction onError)
    {
        var result = await _api.SendAsync<List<EventFormBase>?>(HttpMethod.Get, $"api/events/admin/delta?since={_lastUpdated:yyyy-MM-dd HH:mm:ss}", onError: onError);
        if(result is not null && result.Count > 0)
        {
            await ComputeChangesAndUpdates(result);   
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            await SaveCache();
        }
        _lastUpdated = DateTime.Now;
    }

    private async Task ComputeChangesAndUpdates(List<EventFormBase> incoming, bool suppressRefresh = false)
    {
        using DebugTimer _ = new("Computing Changes and Updates", _logging);
        var newEvents = incoming
            .Where(evt => AllEvents.All(existing => existing.id != evt.id))
            .ToList();
        Debug.WriteLine($"{newEvents.Count} are not cached events.");

        var changes = incoming
            .Except(newEvents)
            .Where(evt => AllEvents.All(existing => !existing.IsSameAs(evt))).ToList();
        Debug.WriteLine($"{changes.Count} Events that are different.");

        var toReplace = AllEvents.Where(evt => changes.Any(update => update.id == evt.id)).ToList();

        AllEvents = [.. AllEvents.Except(toReplace).Union(changes).Union(newEvents)];
        if(!suppressRefresh)
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        await SaveCache();
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(100);
    }

    public async Task<List<EventFormBase>> GetPendingEventsAsync(ErrorAction onError)
    {
        var result = await _api.SendAsync<List<EventFormBase>?>(HttpMethod.Get, "api/events/admin/pending-events", onError: onError);
        if(result is not null)
        {
            await ComputeChangesAndUpdates(result);
        }
        return result ?? [];
    }

    public async Task<List<EventFormBase>> GetTwoWeekListAsync(ErrorAction onError)
    {
        var result = await _api.SendAsync<List<EventFormBase>?>(HttpMethod.Get, "api/events/admin/two-week-list", onError: onError);

        return result ?? [];
    }

    public async Task<List<EventFormBase>> GetAllEvents(ErrorAction onError, DateTime start, DateTime end, bool suppressRefresh = false)
    {
        if(suppressRefresh)
            return [.. AllEvents.Where(evt => evt.start >= start && evt.end <= end)];

        if (end < CacheStartDate)
            end = CacheStartDate;

        if(start > CacheEndDate)
            start = CacheEndDate;

        if (CacheStartDate > start)
            CacheStartDate = start;

        var result = await _api.SendAsync<List<EventFormBase>?>(HttpMethod.Get,
            $"api/events/admin/all-events?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}", onError: onError);

        if (result is not null)
        {
            await ComputeChangesAndUpdates(result);
        }

        return result ?? [];
    }

    public async Task<Optional<EventFormBase>> ApproveEvent(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{eventId}/approve", onError: onError);
        if(result is not null)
        {
            await ComputeChangesAndUpdates([result]);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{result.summary} - {result.start:yyyy-MM-dd} was approved.");
            return Optional<EventFormBase>.Some(result);
        }

        return Optional<EventFormBase>.None();
    }
    public async Task<Optional<EventFormBase>> DeclineEvent(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{eventId}/decline", onError: onError);
        if (result is not null)
        {
            await ComputeChangesAndUpdates([result]);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{result.summary} - {result.start:yyyy-MM-dd} was declined.");
            return Optional<EventFormBase>.Some(result);
        }

        return Optional<EventFormBase>.None();
    }

    public async Task SendNote(string eventId, CreateApprovalNote note, ErrorAction onError)
    {
        var evt = AllEvents.FirstOrDefault(evt=> evt.id == eventId);
        await _api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/approve", JsonSerializer.Serialize(note), onError: onError);

        if(evt is not null) 
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Note submitted to event {evt.summary} - {evt.start:yyyy-MM-dd}.");
        else
            _logging.LogError(new("Event Not Found When sending note", eventId));
    }

    public async Task<List<EventApprovalStatusRecord>> GetHistory(string eventId, ErrorAction onError)
    {
        var result = await _api.SendAsync<List<EventApprovalStatusRecord>?>(HttpMethod.Get,
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
