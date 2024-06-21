using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services;

public class LocationService :
    IAsyncInitService,
    ICacheService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public ImmutableArray<Location> OnCampusLocations { get; private set; } = [];
    public ImmutableArray<Location> MyCustomLocations { get; private set; } = [];

    public LocationService(LocalLoggingService logging, ApiService api)
    {
        _logging = logging;
        _api = api;
    }

    public async Task<Location?> CreateCustomLocation(string name, bool isPublic, ErrorAction onError)
    {
        var result = await _api.SendAsync<Location?>(HttpMethod.Post, $"api/events/location/custom?name={name}&isPublic={isPublic}", onError: onError);
        if(result.HasValue)
        {
            MyCustomLocations = MyCustomLocations.Add(result.Value);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
        return result;
    }

    public async Task DeleteCustomLocation(string id, ErrorAction onError)
    {
        var location = MyCustomLocations.FirstOrDefault(loc => loc.id == id);
        bool success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/events/location/custom/{id}", 
            onError: err =>
            {
                onError(err);
                success = false;
            });

        if(success)
        {
            MyCustomLocations = MyCustomLocations.Remove(location);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
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

        var campusTask = _api.SendAsync<ImmutableArray<Location>>(HttpMethod.Get, "api/events/location", onError: onError);
        campusTask.WhenCompleted(() =>
        {
            OnCampusLocations = campusTask.Result;
            Progress += 0.5;
        });

        var customTask = _api.SendAsync<ImmutableArray<Location>>(HttpMethod.Get, "api/events/location/custom", onError: onError);
        customTask.WhenCompleted(() =>
        {
            MyCustomLocations = customTask.Result;
            Progress += 0.5;
        });

        await Task.WhenAll(campusTask, customTask);

        Ready = true;
    }

    public async Task Refresh(ErrorAction onError)
    {
        MyCustomLocations = await _api.SendAsync<ImmutableArray<Location>>(HttpMethod.Get, "api/events/location/custom", onError: onError);
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }
}
