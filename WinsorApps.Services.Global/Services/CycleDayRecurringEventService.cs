using AsyncAwaitBestPractices;
using System.Text.Json;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Global.Services
{
    /// <summary>
    /// This service is /not/ included in the InitializeGlobalServices Dependency Injection extension.
    /// Please manually add this service if it is relevant to an app~
    /// </summary>
    public partial class CycleDayRecurringEventService : 
        IAsyncInitService, 
        ICacheService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public event EventHandler? OnCacheRefreshed;

        public string CacheFileName => ".cdre.cache";
        public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }
        public async Task SaveCache() 
        {
            await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", JsonSerializer.Serialize(RecurringEvents));
        }
        public bool LoadCache() 
        {
            if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
                return false;

            var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
            try
            {
                RecurringEvents = JsonSerializer.Deserialize<List<CycleDayRecurringEvent>>(json) ?? [];
            }
            catch
            {
                return false;
            }

            return true;
        }
        public List<CycleDayRecurringEvent> RecurringEvents { get; private set; } = [];

        public List<CycleDayRecurringEvent> OpenEventList =>
            [.. RecurringEvents.Where(evt => evt.ending > DateOnly.FromDateTime(DateTime.Today))];

        public CycleDayRecurringEventService(ApiService api, LocalLoggingService logging)
        {
            _api = api;
            _logging = logging;
            _api.OnLoginSuccess += (_, _) => Initialize(_logging.LogError).SafeFireAndForget(e => e.LogException(_logging));
        }

        public bool Started { get; private set; }

        public bool Ready { get; private set; }

        public double Progress { get; private set; }

        public async Task Initialize(ErrorAction onError)
        {
            while (!_api.Ready)
                await Task.Delay(250);
            Started = true;

            if (LoadCache())
            {
                Refresh(onError).SafeFireAndForget(e => e.LogException(_logging));
            }
            else
            {
                RecurringEvents = await _api.SendAsync<List<CycleDayRecurringEvent>>(HttpMethod.Get, "api/users/self/cycle-day-recurring-events", onError: onError)
                    ?? [];
                await SaveCache();
            }
            Progress = 1;

            Ready = true;
        }

        public async Task Refresh(ErrorAction onError)
        {
            var result = await _api.SendAsync<List<CycleDayRecurringEvent>>(HttpMethod.Get, "api/users/self/cycle-day-recurring-events", onError: onError) ?? [];
            if(!result.SequenceEqual(RecurringEvents))
            {
                RecurringEvents = result;
                await SaveCache();
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(250);
        }

        public async Task<CycleDayRecurringEvent?> CreateNewEvent(CreateRecurringEvent newEvent, ErrorAction onError)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, "called to create new event");
            var result = await _api.SendAsync<CreateRecurringEvent, CycleDayRecurringEvent?>(HttpMethod.Post, "api/users/self/cycle-day-recurring-events", newEvent, onError: onError);
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, "new event create attempted");
            if (result is not null)
            {
                RecurringEvents.Add(result);
                await SaveCache();
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
                _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{newEvent.summary} created");
            }
            else
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, $"user entered {newEvent}");
            }
            return result;
        }

        public async Task<CycleDayRecurringEvent?> UpdateEvent(string eventId, CreateRecurringEvent newEvent, ErrorAction onError)
        {
            var result = await _api.SendAsync<CreateRecurringEvent, CycleDayRecurringEvent?>(HttpMethod.Put, $"api/users/self/cycle-day-recurring-events/{eventId}", newEvent, onError: onError);

            var oldEvent = RecurringEvents.FirstOrDefault(evt => evt.id == eventId);

            if (result is not null)
            {
                if (!string.IsNullOrEmpty(oldEvent?.id))
                    RecurringEvents.Remove(oldEvent);
                RecurringEvents.Add(result);
                await SaveCache();
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }

            return result;
        }

        public async Task DeleteEvent(string eventId, ErrorAction onError)
        {
            await _api.SendAsync(HttpMethod.Delete, $"api/users/self/cycle-day-recurring-events/{eventId}", onError: onError);
            RecurringEvents.Remove(RecurringEvents.First(evt => evt.id == eventId));
            await SaveCache();
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
    }
}
