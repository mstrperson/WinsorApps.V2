using AsyncAwaitBestPractices;
using System.Collections.Immutable;
using System.Text.Json;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services
{
    public class TheaterService : IAsyncInitService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;
        public string CacheFileName => ".theater.cache";
        public async Task SaveCache()
        {
            var json = JsonSerializer.Serialize(AvailableMenus);
            await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", json);
        }

        public bool LoadCache()
        {
            if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
                return false;

            try
            {
                var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
                AvailableMenus = JsonSerializer.Deserialize<ImmutableArray<TheaterMenuCategory>>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public ImmutableArray<TheaterMenuCategory> AvailableMenus { get; private set; } = [];

        public TheaterService(ApiService api, LocalLoggingService logging)
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
            if (Ready)
                return;

            await _api.WaitForInit(onError);
            if (Started)
                return;

            Started = true;
            if (!LoadCache())
            {
                AvailableMenus = await _api.SendAsync<ImmutableArray<TheaterMenuCategory>?>(HttpMethod.Get,
                    "api/events/theater/menu", onError: onError) ?? [];
                await SaveCache();
            }
            Progress = 1;
            Ready = true;
        }

        public async Task Refresh(ErrorAction onError)
        {

            AvailableMenus = await _api.SendAsync<ImmutableArray<TheaterMenuCategory>?>(HttpMethod.Get,
                "api/events/theater/menu", onError: onError) ?? [];
            await SaveCache();
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(250);
        }
    }
}
