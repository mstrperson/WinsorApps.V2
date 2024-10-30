using AsyncAwaitBestPractices;
using System.Collections.Immutable;
using System.Text.Json;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services
{
    public class ContactService : 
        IAsyncInitService,
        ICacheService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public event EventHandler? OnCacheRefreshed;

        public string CacheFileName => ".contacts.cache";
        public void SaveCache()
        {
            var json = JsonSerializer.Serialize(MyContacts);
            File.WriteAllText($"{_logging.AppStoragePath}{CacheFileName}", json);
        }

        public bool LoadCache()
        {
            if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
                return false;

            try
            {
                var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
                MyContacts = JsonSerializer.Deserialize<ImmutableArray<Contact>>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public ContactService(LocalLoggingService logging, ApiService api)
        {
            _logging = logging;
            _api = api;

            _api.OnLoginSuccess += (_, _) => Initialize(_logging.LogError).SafeFireAndForget(e => e.LogException(_logging));
        }

        public ImmutableArray<Contact> MyContacts { get; private set; } = [];

        public async Task<Contact> CreateNewContact(NewContact newContact, ErrorAction onError)
        {
            var result = await _api.SendAsync<NewContact, Contact?>(HttpMethod.Post, "api/users/self/contacts", newContact, onError: onError);
            if (!result.HasValue)
                return Contact.Empty;

            MyContacts.Add(result.Value);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            return result.Value;
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
                MyContacts = await _api.SendAsync<ImmutableArray<Contact>?>(HttpMethod.Get, "api/users/self/contacts", onError: onError) ?? [];
                SaveCache();
            }
            Progress = 1;
            Ready = true;
        }

        public async Task Refresh(ErrorAction onError)
        {
            MyContacts = await _api.SendAsync<ImmutableArray<Contact>?>(HttpMethod.Get, "api/users/self/contacts", onError: onError) ?? [];
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            SaveCache();
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(250);
        }
    }
}
