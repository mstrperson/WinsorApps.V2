using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.EventForms.Models;

namespace WinsorApps.Services.EventForms.Services
{
    public partial class EventFormsService
    {

        public async Task<EventFormBase> GetEvent(string id, ErrorAction onError, bool ignoreCache = false)
        {
            if (!ignoreCache && EventsCache.Any(evt => evt.id == id))
                return EventsCache.First(evt => evt.id == id);

            var evt = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{id}", onError: onError);
            if (!evt.HasValue)
                return EventFormBase.Empty;

            if (!EventsCache.Any(e => e.id == evt.Value.id))
            {
                EventsCache = EventsCache.Add(evt.Value);
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }
            return evt.Value;
        }

        /// <summary>
        /// Barring some Error that prevents the event from being initiated,
        /// Creates a new event and returns the event in the "creating" state.
        /// </summary>
        /// <param name="newEvent"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<EventFormBase> StartEventCreation(NewEvent newEvent, ErrorAction onError)
        {
            var created = await _api.SendAsync<NewEvent, EventFormBase?>(HttpMethod.Post, "api/events", newEvent, onError: onError);
            if (!created.HasValue)
                return EventFormBase.Empty;

            EventsCache = EventsCache.Add(created.Value);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            return created.Value;
        }

        /// <summary>
        /// Starts the update process.  Updates Event Base Details and sets the status to "updating".
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updated"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<EventFormBase> StartEventUpdate(string id, NewEvent updated, ErrorAction onError)
        {
            var oldEvent = EventsCache.FirstOrDefault(evt => evt.id == id);

            var result = await _api.SendAsync<NewEvent, EventFormBase?>(HttpMethod.Put, $"api/events/{id}", updated, onError: onError);
            if (!result.HasValue)
                return EventFormBase.Empty;

            if (oldEvent == default)
                EventsCache = EventsCache.Add(result.Value);
            else
                EventsCache = EventsCache.Replace(oldEvent, result.Value);

            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            return result.Value;
        }

        public async Task DeleteEvent(string id, ErrorAction onError)
        {
            var existing = EventsCache.FirstOrDefault(evt => evt.id == id);
            bool success = true;
            await _api.SendAsync(HttpMethod.Delete, $"api/events/{id}", onError: err =>
            {
                success = false;
                onError(err);
            });

            if (success)
            {
                EventsCache = EventsCache.Remove(existing);
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task<EventFormBase> CompleteSubmission(string id, ErrorAction onError)
        {
            var existing = EventsCache.FirstOrDefault(evt => evt.id == id);
            bool success = true;
            var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{id}/complete-submission", onError: err =>
            {
                success = false;
                onError(err);
            });

            if (!result.HasValue)
                return EventFormBase.Empty;

            if(success)
            {
                EventsCache = EventsCache.Replace(existing, result.Value);
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }

            return result.Value;
        }

        public async Task<EventFormBase> CompleteUpdate(string id, ErrorAction onError)
        {
            var existing = EventsCache.FirstOrDefault(evt => evt.id == id);
            bool success = true;
            var result = await _api.SendAsync<EventFormBase?>(HttpMethod.Get, $"api/events/{id}/complete-update", onError: err =>
            {
                success = false;
                onError(err);
            });

            if (!result.HasValue)
                return EventFormBase.Empty;

            if (success)
            {
                EventsCache = EventsCache.Replace(existing, result.Value);
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }

            return result.Value;
        }
    }
}
