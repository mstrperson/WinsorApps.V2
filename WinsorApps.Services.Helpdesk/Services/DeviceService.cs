using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.Services.Helpdesk.Services;

public sealed class DeviceService : IAsyncInitService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    public bool Ready { get; private set; } = false;

    private List<DeviceRecord>? _loaners;
    public ImmutableList<DeviceRecord> Loaners
    {
        get
        {

            if (!Ready || _loaners is null)
                throw new ServiceNotReadyException(_logging, "Device Cache has not been populated yet.");

            return _loaners.ToImmutableList();
        }
    }

    private List<DeviceRecord>? _deviceCache;
    public ImmutableList<DeviceRecord> DeviceCache
    {
        get
        {
            if (!Ready || _deviceCache is null)
                throw new ServiceNotReadyException(_logging, "Device Cache has not been populated yet.");

            return _deviceCache.ToImmutableList();
        }
    }

    private List<WinsorDeviceRecord>? _winsorDeviceCache;
    public ImmutableList<WinsorDeviceRecord> WinsorDeviceCache
    {
        get
        {

            if (!Ready || _winsorDeviceCache is null)
                throw new ServiceNotReadyException(_logging, "Device Cache has not been populated yet.");

            return _winsorDeviceCache.ToImmutableList();
        }
    }

    private ImmutableArray<DeviceCategoryRecord>? _categories;

    public DeviceService(ApiService api, LocalLoggingService logging)
    {
        _api = api;
        _logging = logging;
    }

    public ImmutableArray<DeviceCategoryRecord> Categories
    {
        get
        {
            if (!Ready)
                throw new ServiceNotReadyException(_logging, "Categories have not yet been populated.");

            return _categories!.Value;
        }
    }

    public bool Started { get; private set; }

    public double Progress { get; private set; } = 0;

    public async Task WaitForInit(ErrorAction onError)
    {
        if (Ready) return;

        if (!this.Started)
            await this.Initialize(onError);

        while (!this.Ready)
        {
            await Task.Delay(250);
        }
    }
    public async Task Refresh(ErrorAction onError)
    {
        Started = false;
        Progress = 0;
        await Initialize(onError);
    }

    public async Task Initialize(ErrorAction onError)
    {
        if (Started)
            return;

        Started = true;

        var loanersTask = _api.SendAsync<List<DeviceRecord>>(HttpMethod.Get,
            "api/devices/loaners", onError: onError);
        loanersTask.WhenCompleted(() =>
        {
            Progress += 1 / 3.0;
            _loaners = loanersTask.Result;
        });

        var devicesTask = _api.SendAsync<List<DeviceRecord>>(HttpMethod.Get,
            "api/devices", onError: onError);

        devicesTask.WhenCompleted(() =>
        {
            Progress += 1 / 3.0;
            _deviceCache = devicesTask.Result;
        });

        var categoriesTask = _api.SendAsync<ImmutableArray<DeviceCategoryRecord>>(HttpMethod.Get,
            "api/devices/categories", onError: onError);
        categoriesTask.WhenCompleted(() =>
        {
            Progress += 1 / 3.0;
            _categories = categoriesTask.Result;
        });

        _winsorDeviceCache = new();

        await Task.WhenAll(loanersTask, devicesTask, categoriesTask);

        Progress = 1;
        Ready = true;
    }

    public async Task<DeviceRecord?> UpdateDevice(string deviceId, UpdateDeviceRecord update, ErrorAction onError)
    {
        var result = await _api.SendAsync<UpdateDeviceRecord, DeviceRecord?>(HttpMethod.Put,
            $"api/devices/{deviceId}", update, onError: onError);


        if (result.HasValue && DeviceCache.Any(dev => dev.id == result.Value.id))
            _deviceCache!.Replace(DeviceCache.First(dev => dev.id == result.Value.id), result.Value);
        else if (result.HasValue)
            _deviceCache!.Add(result.Value);

        return result;
    }

    public async Task<DeviceRecord?> CreateNewDeviceRecord(CreateDeviceRecord newDevice, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateDeviceRecord, DeviceRecord?>(HttpMethod.Post, "api/devices", newDevice,
            onError: onError);

        if (result.HasValue && DeviceCache.Any(dev => dev.id == result.Value.id))
            _deviceCache!.Replace(DeviceCache.First(dev => dev.id == result.Value.id), result.Value);
        else if (result.HasValue)
            _deviceCache!.Add(result.Value);

        return result;
    }
    public async Task<DeviceRecord?> SearchDevices(string identifier, ErrorAction onError)
    {
        if (!Ready)
        {
            await Task.Run(async () =>
            {
                while (!Ready)
                {
                    await Task.Delay(500);
                }
            });
        }

        if (DeviceCache.Any(dev =>
            dev.serialNumber.ToUpperInvariant() == identifier ||
            (dev.winsorDevice?.assetTag.ToUpperInvariant() ?? string.Empty) == identifier))
        {
            return DeviceCache.First(dev =>
                dev.serialNumber.ToUpperInvariant() == identifier ||
                (dev.winsorDevice?.assetTag.ToUpperInvariant() ?? string.Empty) == identifier)!;
        }

        var results = await _api.SendAsync<ImmutableArray<DeviceRecord>>(HttpMethod.Get,
                $"api/devices?serialNumberPattern={identifier}", onError: onError);
        if (!results.Any())
            results = await _api.SendAsync<ImmutableArray<DeviceRecord>>(HttpMethod.Get,
                $"api/devices?assetTagPattern={identifier}", onError: onError);

        if (!results.Any())
            return null;

        foreach (var result in results)
        {
            if (DeviceCache.All(dev => dev.id != result.id))
                _deviceCache!.Add(result);
        }

        return results.First();
    }

    public async Task<WinsorDeviceRecord?> GetWinsorDeviceDetails(string id, ErrorAction? onError = null)
    {
        if (WinsorDeviceCache.Any(dev => dev.id == id))
            return WinsorDeviceCache.First(dev => dev.id == id);

        var result = await _api.SendAsync<WinsorDeviceRecord?>(HttpMethod.Get,
            $"api/{id}/winsor-device-info", onError: onError);
        return result;
    }

    public async Task<bool> DisposeDevice(string id, ErrorAction onError)
    {

        var result = await _api.SendAsync<DeviceRecord?>(HttpMethod.Delete,
            $"api/devices/{id}/dispose-inventory", onError: onError);
        if (!result.HasValue) return false;

        var dev = DeviceCache.First(d => d.id == id);
        _deviceCache!.Replace(dev, result.Value);
        return true;
    }
}