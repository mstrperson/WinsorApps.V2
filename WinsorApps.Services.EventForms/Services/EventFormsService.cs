using AsyncAwaitBestPractices;
using System.Collections.Immutable;
using System.Text.Json;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services
{
    public partial class EventFormsService :
        IAsyncInitService,
        IAutoRefreshingCacheService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public EventFormsService(LocalLoggingService logging, ApiService api)
        {
            _logging = logging;
            _api = api;
            _api.OnLoginSuccess += (_, _) => Initialize(_logging.LogError).SafeFireAndForget(e => e.LogException(_logging));
        }

        public record CacheStructure(
            List<EventFormBase> events, 
            List<EventFormBase> leadEvents, 
            List<string> eventTypes,
            List<ApprovalStatus> statusLabels,
            List<VehicleCategory> vehicleCategories);

        public string CacheFileName => ".event-base.cache";
        public async Task SaveCache()
        {
            var cache = new CacheStructure(EventsCache, LeadEvents, EventTypes, StatusLabels, VehicleCategories);
            var json = JsonSerializer.Serialize(cache);
            await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", json);
        }

        public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }

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
                if (cache is null) return false;
                EventsCache = cache.events;
                LeadEvents = cache.leadEvents;
                EventTypes = cache.eventTypes;
                StatusLabels = cache.statusLabels;
                VehicleCategories = cache.vehicleCategories;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<EventFormBase> EventsCache { get; protected set; } = [];

        public List<EventFormBase> LeadEvents { get; private set; } = [];

        public List<string> EventTypes { get; private set; } = [];

        public List<ApprovalStatus> StatusLabels { get; private set; } = [];

        public List<VehicleCategory> VehicleCategories { get; private set; } = [];


        public bool Started { get; protected set; }

        public bool Ready { get; protected set; }

        public double Progress { get; protected set; }

        public TimeSpan RefreshInterval => TimeSpan.FromMinutes(30);

        public bool Refreshing { get; private set; }

        public event EventHandler? OnCacheRefreshed;

        public DateTime CacheStartDate { get; protected set; } = DateTime.Today;
        public DateTime CacheEndDate { get; protected set; } = DateTime.Today;

        public async Task Initialize(ErrorAction onError)
        {
            if (Started)
                return;

            while (!_api.Ready)
                await Task.Delay(250);

            Started = true;

            if (!LoadCache())
            {
                double taskCount = 5;

                var statusTask = _api.SendAsync<List<ApprovalStatus>?>(HttpMethod.Get, "api/events/status-labels", onError: onError);
                statusTask.WhenCompleted(() =>
                {
                    StatusLabels = statusTask.Result ?? [];
                    Progress += 1 / taskCount;
                });

                var typeTask = _api.SendAsync<List<string>?>(HttpMethod.Get, "api/events/event-types", onError: onError);
                typeTask.WhenCompleted(() =>
                {
                    EventTypes = typeTask.Result ?? [];
                    Progress += 1 / taskCount;
                });

                var vehicleTask = _api.SendAsync<List<VehicleCategory>?>(HttpMethod.Get, "api/events/transportation/vehicles", onError: onError);
                vehicleTask.WhenCompleted(() =>
                {
                    VehicleCategories = vehicleTask.Result ?? [];
                    Progress += 1 / taskCount;
                });

                var createdTask = _api.SendAsync<List<EventFormBase>?>(HttpMethod.Get, "api/events/created", onError: onError);
                createdTask.WhenCompleted(() =>
                {
                    EventsCache = createdTask.Result ?? [];
                    Progress += 1 / taskCount;
                });

                var leadTask = _api.SendAsync<List<EventFormBase>?>(HttpMethod.Get, "api/events/lead", onError: onError);
                leadTask.WhenCompleted(() =>
                {
                    LeadEvents = leadTask.Result ?? [];
                    Progress += 1 / taskCount;
                });


                await Task.WhenAll(statusTask, typeTask, vehicleTask, createdTask, leadTask);

                if (EventsCache.Count != 0)
                {
                    CacheStartDate = EventsCache.Select(evt => evt.start).Min();
                    CacheEndDate = EventsCache.Select(evt => evt.start).Max();
                }
                Ready = true;
                await SaveCache();
            }
            Ready = true;
            Progress = 1;

            //RefreshInBackground(CancellationToken.None, onError).SafeFireAndForget(e => e.LogException(_logging));
        }

        public async Task Refresh(ErrorAction onError)
        {
            Refreshing = true;
            var updated = 
                await _api.SendAsync<string[], List<EventFormBase>>(HttpMethod.Get, "api/events/list",
                    EventsCache.Select(evt => evt.id).ToArray(), onError: onError) ?? [];
            EventsCache = updated;
            Refreshing = false;
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            await SaveCache();
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(250);
        }

        public async Task RefreshInBackground(CancellationToken token, ErrorAction onError)
        {
            while(!token.IsCancellationRequested)
            {
                await Task.Delay(RefreshInterval);
                await Refresh(onError);
            }
        }
    }
}
