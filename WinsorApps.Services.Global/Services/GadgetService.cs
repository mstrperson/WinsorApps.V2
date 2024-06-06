using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Global.Services
{
    public partial class GadgetService : IAsyncInitService, ICacheService
    {
        private readonly ApiService _api;

        public event EventHandler? OnCacheRefreshed;

        public ImmutableArray<CycleDayRecurringEvent> RecurringEvents { get; private set; } = [];

        public GadgetService(ApiService api)
        {
            _api = api;
        }

        public bool Started { get; private set; }

        public bool Ready { get; private set; }

        public double Progress { get; private set; }

        public async Task Initialize(ErrorAction onError)
        {
            while (!_api.Ready)
                await Task.Delay(250);
            Started = true;

            RecurringEvents = await _api.SendAsync<ImmutableArray<CycleDayRecurringEvent>>(HttpMethod.Get, "api/users/self/cycle-day-recurring-events", onError: onError);
            Progress = 1;

            Ready = true;
        }

        public async Task Refresh(ErrorAction onError)
        {
            var result = await _api.SendAsync<ImmutableArray<CycleDayRecurringEvent>>(HttpMethod.Get, "api/users/self/cycle-day-recurring-events", onError: onError);
            if(!result.SequenceEqual(RecurringEvents))
            {
                RecurringEvents = result;
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
            var result = await _api.SendAsync<CreateRecurringEvent, CycleDayRecurringEvent?>(HttpMethod.Post, "api/users/self/cycle-day-recurring-events", newEvent, onError: onError);

            if(result.HasValue)
            {
                RecurringEvents = RecurringEvents.Add(result.Value);
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }

            return result;
        }

        public async Task<CycleDayRecurringEvent> UpdateEvent(string eventId, CreateRecurringEvent newEvent, ErrorAction onError)
        {
            var result = await _api.SendAsync<CreateRecurringEvent, CycleDayRecurringEvent?>(HttpMethod.Put, $"api/users/self/cycle-day-recurring-events/{eventId}", newEvent, onError: onError);

            var oldEvent = RecurringEvents.FirstOrDefault(evt => evt.id == eventId);

            if (result.HasValue)
            {
                if (!string.IsNullOrEmpty(oldEvent.id))
                    RecurringEvents = RecurringEvents.Remove(oldEvent);
                RecurringEvents = RecurringEvents.Add(result.Value);
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }

            return result ?? oldEvent;
        }
    }
}
