using AsyncAwaitBestPractices;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            MenuCategories = await _api.SendAsync<ImmutableArray<CateringMenuCategory>>(HttpMethod.Get, "api/events/catering/menu");
            Progress = 1;
            Ready = true;
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        public async Task Refresh(ErrorAction onError)
        {
            MenuCategories = await _api.SendAsync<ImmutableArray<CateringMenuCategory>>(HttpMethod.Get, "api/events/catering/menu");
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(250);
        }
    }
}
