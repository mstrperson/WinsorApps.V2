﻿using AsyncAwaitBestPractices;
using System.Collections.Immutable;
using System.Text.Json;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services
{
    public class ReadonlyCalendarService : IAsyncInitService, IAutoRefreshingCacheService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public List<CalendarEvent<EventFormBase>> EventForms { get; private set; } = [];
        public List<CalendarEvent<CateringEvent>> CateringEvents { get; private set; } = [];
        public List<CalendarEvent<FacilitiesEvent>> FacilitiesEvents { get; private set; } = [];
        public List<CalendarEvent<TechEvent>> TechEvents { get; private set; } = [];
        public List<CalendarEvent<TheaterEvent>> TheaterEvents { get; private set; } = [];
        public List<CalendarEvent<MarCommRequest>> MarCommEvents { get; private set; } = [];
        public List<CalendarEvent<FieldTrip>> FieldTripEvents { get; private set; } = [];

        public record CacheStructure(
            List<CalendarEvent<EventFormBase>> eventForms,
            List<CalendarEvent<CateringEvent>> catering,
            List<CalendarEvent<FacilitiesEvent>> facilities,
            List<CalendarEvent<TechEvent>> tech,
            List<CalendarEvent<TheaterEvent>> theater,
            List<CalendarEvent<MarCommRequest>> marcom,
            List<CalendarEvent<FieldTrip>> fieldTrips);

        public string CacheFileName => ".readonly-calendar.cache";
        public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }
        public async Task SaveCache()
        {
            var cache = new CacheStructure(EventForms, CateringEvents, FacilitiesEvents, TechEvents, TheaterEvents, MarCommEvents, FieldTripEvents);
            var json = JsonSerializer.Serialize(cache);
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
                var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
                var cache = JsonSerializer.Deserialize<CacheStructure>(json);
                if (cache is null) return false;
                EventForms = cache.eventForms;
                CateringEvents = cache.catering;
                FacilitiesEvents = cache.facilities;
                TechEvents = cache.tech;
                MarCommEvents = cache.marcom;
                FieldTripEvents = cache.fieldTrips;
                TheaterEvents = cache.theater;


                return true;
            }
            catch
            {
                return false;
            }
        }
        public ReadonlyCalendarService(LocalLoggingService logging, ApiService api)
        {
            _logging = logging;
            _api = api;
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
            if (!LoadCache())
            {
                await ManualLoadData(onError);
            }
            else
                Refresh(onError).SafeFireAndForget(e => e.LogException(_logging));

            Progress = 1;
            Ready = true;

            RefreshInBackground(CancellationToken.None, onError).SafeFireAndForget(e => e.LogException(_logging));
        }

        private async Task ManualLoadData(ErrorAction onError)
        {
            int year = DateTime.Today.Month > 6 ? DateTime.Today.Year : DateTime.Today.Year - 1;

            var allEventsTask = _api.SendAsync<List<CalendarEvent<EventFormBase>>?>(HttpMethod.Get, $"api/events/calendar?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var cateringTask = _api.SendAsync<List<CalendarEvent<CateringEvent>>?>(HttpMethod.Get, $"api/events/calendar/catering?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var facilitiesTask = _api.SendAsync<List<CalendarEvent<FacilitiesEvent>>?>(HttpMethod.Get, $"api/events/facilities?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var technologyTask = _api.SendAsync<List<CalendarEvent<TechEvent>>?>(HttpMethod.Get, $"api/events/calendar/technology?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var theaterTask = _api.SendAsync<List<CalendarEvent<TheaterEvent>>?>(HttpMethod.Get, $"api/events/calendar/theater?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var marcomTask = _api.SendAsync<List<CalendarEvent<MarCommRequest>>?>(HttpMethod.Get, $"api/events/calendar/marcom?start={year}-07-01&end={year + 1}-06-30", onError: onError);
            var fieldTripTask = _api.SendAsync<List<CalendarEvent<FieldTrip>>?>(HttpMethod.Get, $"api/events/calendar/field-trip?start={year}-07-01&end={year + 1}-06-30", onError: onError);

            allEventsTask.WhenCompleted(() =>
            {
                EventForms = allEventsTask.Result ?? [];
                Progress += 1.0 / 7;

            },
            () =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Downloading `allEventsTask` ended with a Canceled state...");
                EventForms = [];
                Progress += 1.0 / 7;
            });
            allEventsTask.SafeFireAndForget(e => e.LogException(_logging));

            cateringTask.WhenCompleted(() =>
            {
                CateringEvents = cateringTask.Result ?? [];
                Progress += 1.0 / 7;
            },
            () =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Downloading `cateringTask` ended with a Canceled state...");
                EventForms = [];
                Progress += 1.0 / 7;
            });
            cateringTask.SafeFireAndForget(e => e.LogException(_logging));

            facilitiesTask.WhenCompleted(() =>
            {
                FacilitiesEvents = facilitiesTask.Result ?? [];
                Progress += 1.0 / 7;
            },
            () =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Downloading `facilitiesTask` ended with a Canceled state...");
                EventForms = [];
                Progress += 1.0 / 7;
            });
            facilitiesTask.SafeFireAndForget(e => e.LogException(_logging));

            technologyTask.WhenCompleted(() =>
            {
                TechEvents = technologyTask.Result ?? [];
                Progress += 1.0 / 7;
            },
            () =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Downloading `technologyTask` ended with a Canceled state...");
                EventForms = [];
                Progress += 1.0 / 7;
            });
            technologyTask.SafeFireAndForget(e => e.LogException(_logging));

            theaterTask.WhenCompleted(() =>
            {
                TheaterEvents = theaterTask.Result ?? [];
                Progress += 1.0 / 7;
            },
            () =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Downloading `theaterTask` ended with a Canceled state...");
                EventForms = [];
                Progress += 1.0 / 7;
            });
            theaterTask.SafeFireAndForget(e => e.LogException(_logging));

            marcomTask.WhenCompleted(() =>
            {
                MarCommEvents = marcomTask.Result ?? [];
                Progress += 1.0 / 7;
            },
            () =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Downloading `marcomTask` ended with a Canceled state...");
                EventForms = [];
                Progress += 1.0 / 7;
            });
            marcomTask.SafeFireAndForget(e => e.LogException(_logging));

            fieldTripTask.WhenCompleted(() =>
            {
                FieldTripEvents = fieldTripTask.Result ?? [];
                Progress += 1.0 / 7;
            },
            () =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Downloading `fieldTripTask` ended with a Canceled state...");
                EventForms = [];
                Progress += 1.0 / 7;
            });
            fieldTripTask.SafeFireAndForget(e => e.LogException(_logging));

            await Task.WhenAll(allEventsTask, cateringTask, facilitiesTask, technologyTask, theaterTask, marcomTask, fieldTripTask);
            await SaveCache();
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
            await ManualLoadData(onError);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
    }
}
