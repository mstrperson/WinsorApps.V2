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
    public class TheaterService : IAsyncInitService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

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

            AvailableMenus = await _api.SendAsync<ImmutableArray<TheaterMenuCategory>>(HttpMethod.Get,
                "api/events/theater/menu", onError: onError);

            Progress = 1;
            Ready = true;
        }

        public async Task Refresh(ErrorAction onError)
        {

            AvailableMenus = await _api.SendAsync<ImmutableArray<TheaterMenuCategory>>(HttpMethod.Get,
                "api/events/theater/menu", onError: onError);
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(250);
        }
    }
}
