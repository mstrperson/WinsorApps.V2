using AsyncAwaitBestPractices;
using System.Collections.Immutable;
using System.Text.Json;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services
{
    public class CateringMenuService : IAsyncInitService, ICacheService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public event EventHandler? OnCacheRefreshed;

        public ImmutableArray<CateringMenuCategory> MenuCategories { get; private set; } = [];

        public ImmutableArray<CateringMenuCategory> AvailableCategories => [.. MenuCategories.Where(cat => !cat.isDeleted)];


        public CateringMenuService(LocalLoggingService logging, ApiService api)
        {
            _logging = logging;
            _api = api;

            _api.OnLoginSuccess += (_, _) => Initialize(_logging.LogError).SafeFireAndForget(e => e.LogException(_logging));

        }

        public bool Started { get; private set; }

        public bool Ready { get; private set; }

        public double Progress { get; private set; }

        public async Task Initialize(ErrorAction onError)
        {
            while (!_api.Ready)
                await Task.Delay(250);

            if (Started)
                return;

            Started = true;
            if (!LoadCache())
            {
                MenuCategories = await _api.SendAsync<ImmutableArray<CateringMenuCategory>?>(HttpMethod.Get, "api/events/catering/menu") ?? [];
                SaveCache();
            }
            Progress = 1;
            Ready = true;
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        public async Task Refresh(ErrorAction onError)
        {
            MenuCategories = await _api.SendAsync<ImmutableArray<CateringMenuCategory>?>(HttpMethod.Get, "api/events/catering/menu") ?? [];
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            SaveCache();
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(250);
        }
        public string CacheFileName => ".catering-menu.cache";
        public void SaveCache()
        {
            var json = JsonSerializer.Serialize(MenuCategories);
            File.WriteAllText($"{_logging.AppStoragePath}{CacheFileName}", json);
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
                MenuCategories = JsonSerializer.Deserialize<ImmutableArray<CateringMenuCategory>>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
