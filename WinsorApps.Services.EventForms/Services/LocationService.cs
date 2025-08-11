using System.Text.Json;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services;

public class LocationService(LocalLoggingService logging, ApiService api) :
    IAsyncInitService,
    ICacheService
{
    private readonly ApiService _api = api;
    private readonly LocalLoggingService _logging = logging;

    public List<Location> OnCampusLocations { get; private set; } = [];
    public List<Location> MyCustomLocations { get; private set; } = [];

    public record CacheStructure(List<Location> onCampus, List<Location> custom);

    public string CacheFileName => ".locations.cache";
    public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }
    public async Task SaveCache()
    {
        var cache = new CacheStructure(OnCampusLocations, MyCustomLocations);
        var json = JsonSerializer.Serialize(cache);
        await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", json);
    }

    public bool LoadCache()
    {
        if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
            return false;

        try
        {
            var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
            var cache = JsonSerializer.Deserialize<CacheStructure>(json);
            if(cache is null ) return false;
            OnCampusLocations = cache.onCampus;
            MyCustomLocations = cache.custom;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Location?> CreateCustomLocation(string name, bool isPublic, ErrorAction onError)
    {
        var result = await _api.SendAsync<Location?>(HttpMethod.Post, $"api/events/location/custom?name={name}&isPublic={isPublic}", onError: onError);
        if(result is not null)
        {
            MyCustomLocations.Add(result);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            await SaveCache();
        }
        return result;
    }

    public async Task DeleteCustomLocation(string id, ErrorAction onError)
    {
        var location = MyCustomLocations.FirstOrDefault(loc => loc.id == id);


        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/location/custom/{id}", 
            onError: err =>
            {
                onError(err);
                success = false;
            });

        if (location is null) return;
        if (success)
        {
            MyCustomLocations.Remove(location);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            await SaveCache();
        }
    }

    public bool Started { get; private set; }

    public bool Ready { get; private set; }

    public double Progress { get; private set; }

    public event EventHandler? OnCacheRefreshed;

    public async Task Initialize(ErrorAction onError)
    {
        await _api.WaitForInit(onError);

        Started = true;

        if (!LoadCache())
        {
            var campusTask = _api.SendAsync<List<Location>?>(HttpMethod.Get, "api/events/location", onError: onError);
            campusTask.WhenCompleted(() =>
            {
                OnCampusLocations = campusTask.Result ?? [];
                Progress += 0.5;
            });

            var customTask = _api.SendAsync<List<Location>?>(HttpMethod.Get, "api/events/location/custom", onError: onError);
            customTask.WhenCompleted(() =>
            {
                MyCustomLocations = customTask.Result ?? [];
                Progress += 0.5;
            });

            await Task.WhenAll(campusTask, customTask);
            Ready = true;
            await SaveCache();
        }
        Ready = true;
    }

    public async Task Refresh(ErrorAction onError)
    {
        MyCustomLocations = await _api.SendAsync<List<Location>?>(HttpMethod.Get, "api/events/location/custom", onError: onError) ?? [];
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        await SaveCache();
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }
}
