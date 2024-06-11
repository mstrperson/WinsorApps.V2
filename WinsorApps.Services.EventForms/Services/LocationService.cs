using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.EventForms.Models;
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

    public bool Started { get; private set; }

    public bool Ready { get; private set; }

    public double Progress { get; private set; }

    public event EventHandler? OnCacheRefreshed;

    public Task Initialize(ErrorAction onError)
    {
        throw new NotImplementedException();
    }

    public Task Refresh(ErrorAction onError)
    {
        throw new NotImplementedException();
    }

    public Task WaitForInit(ErrorAction onError)
    {
        throw new NotImplementedException();
    }
}
