using AsyncAwaitBestPractices;
using System.Collections.Immutable;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services
{
    public class ReadonlyCalendarService : IAsyncInitService, IAutoRefreshingCacheService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;
        private readonly RegistrarService _registrar;

        public ImmutableArray<CalendarEvent<EventFormBase>> EventForms { get; private set; } = [];
        public ImmutableArray<CalendarEvent<CateringEvent>> CateringEvents { get; private set; } = [];
        public ImmutableArray<CalendarEvent<FacilitiesEvent>> FacilitiesEvents { get; private set; } = [];
        public ImmutableArray<CalendarEvent<TechEvent>> TechEvents { get; private set; } = [];
        public ImmutableArray<CalendarEvent<TheaterEvent>> TheaterEvents { get; private set; } = [];
        public ImmutableArray<CalendarEvent<MarCommRequest>> MarCommEvents { get; private set; } = [];
        public ImmutableArray<CalendarEvent<FieldTrip>> FieldTripEvents { get; private set; } = [];

        public ReadonlyCalendarService(LocalLoggingService logging, ApiService api, RegistrarService registrar)
        {
            _logging = logging;
            _api = api;
            _registrar = registrar;
            _api.OnLoginSuccess += (_, _) =>
            {
                Initialize(_logging.LogError).SafeFireAndForget(e => e.LogException(_logging));
            };
        }

        public bool Started { get; private set; }

        public bool Ready { get; private set; }

        public double Progress { get; private set; }

        public TimeSpan RefreshInterval => TimeSpan.FromMinutes(15);

        public bool Refreshing { get; private set; }

        public event EventHandler? OnCacheRefreshed;

        public async Task Initialize(ErrorAction onError)
        {
            while (!_api.Ready)
                await Task.Delay(250);

            if (Started)
                return;

            Started = true;

            int year = DateTime.Today.Month > 6 ? DateTime.Today.Year : DateTime.Today.Year - 1;

            var allEventsTask = _api.SendAsync<ImmutableArray<CalendarEvent<EventFormBase>>?>(HttpMethod.Get, $"api/events/calendar?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var cateringTask =  _api.SendAsync<ImmutableArray<CalendarEvent<CateringEvent>>?>(HttpMethod.Get, $"api/events/calendar/catering?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var facilitiesTask =  _api.SendAsync<ImmutableArray<CalendarEvent<FacilitiesEvent>>?>(HttpMethod.Get, $"api/events/facilities?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var technologyTask =  _api.SendAsync<ImmutableArray<CalendarEvent<TechEvent>>?>(HttpMethod.Get, $"api/events/calendar/technology?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var theaterTask =  _api.SendAsync<ImmutableArray<CalendarEvent<TheaterEvent>>?>(HttpMethod.Get, $"api/events/calendar/theater?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var marcomTask =  _api.SendAsync<ImmutableArray<CalendarEvent<MarCommRequest>>?>(HttpMethod.Get, $"api/events/calendar/marcom?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var fieldTripTask =  _api.SendAsync<ImmutableArray<CalendarEvent<FieldTrip>>?>(HttpMethod.Get, $"api/events/calendar/field-trip?start={year}-07-01&end={year + 1}-06-30", onError: onError);

            allEventsTask.WhenCompleted(() =>
            {
                EventForms = allEventsTask.Result ?? [];
                Progress += (1.0 / 7);
            });
            allEventsTask.SafeFireAndForget(e => e.LogException(_logging));

            cateringTask.WhenCompleted(() =>
            {
                CateringEvents = cateringTask.Result ?? [];
                Progress += (1.0 / 7);
            });
            cateringTask.SafeFireAndForget(e => e.LogException(_logging));

            facilitiesTask.WhenCompleted(() =>
            {
                FacilitiesEvents = facilitiesTask.Result ?? [];
                Progress += (1.0 / 7);
            });
            facilitiesTask.SafeFireAndForget(e => e.LogException(_logging));

            technologyTask.WhenCompleted(() =>
            {
                TechEvents = technologyTask.Result ?? [];
                Progress += (1.0 / 7);
            });
            technologyTask.SafeFireAndForget(e => e.LogException(_logging));

            theaterTask.WhenCompleted(() =>
            {
                TheaterEvents = theaterTask.Result ?? [];
                Progress += (1.0 / 7);
            });
            theaterTask.SafeFireAndForget(e => e.LogException(_logging));

            marcomTask.WhenCompleted(() =>
            {
                MarCommEvents = marcomTask.Result ?? [];
                Progress += (1.0 / 7);
            });
            marcomTask.SafeFireAndForget(e => e.LogException(_logging));

            fieldTripTask.WhenCompleted(() =>
            {
                FieldTripEvents = fieldTripTask.Result ?? [];
                Progress += (1.0 / 7);
            });
            fieldTripTask.SafeFireAndForget(e => e.LogException(_logging));

            await Task.WhenAll(allEventsTask, cateringTask, facilitiesTask, technologyTask, theaterTask, marcomTask, fieldTripTask);

            Progress = 1;
            Ready = true;
        }

        public async Task RefreshInBackground(CancellationToken token, ErrorAction onError)
        {
            while(!token.IsCancellationRequested)
            {
                await Task.Delay(RefreshInterval);
                await Refresh(onError);
            }
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(250);
        }

        public async Task Refresh(ErrorAction onError)
        {
            Started = false;
            await Initialize(onError);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
    }
}
