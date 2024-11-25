using System.Text.Json;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.Services.Helpdesk.Services;

public sealed class DeviceService : IAsyncInitService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    public bool Ready { get; private set; } = false;

    private List<DeviceRecord> _loaners = [];
    public ImmutableList<DeviceRecord> Loaners
    {
        get
        {

            if (!Ready || _loaners is null)
                throw new ServiceNotReadyException(_logging, "Device Cache has not been populated yet.");

            return [.. _loaners];
        }
    }

    private List<DeviceRecord> _deviceCache = [];
    public ImmutableList<DeviceRecord> DeviceCache
    {
        get
        {
            if (!Ready || _deviceCache is null)
                throw new ServiceNotReadyException(_logging, "Device Cache has not been populated yet.");

            return [.. _deviceCache];
        }
    }

    private List<WinsorDeviceRecord> _winsorDeviceCache = [];
    public ImmutableList<WinsorDeviceRecord> WinsorDeviceCache
    {
        get
        {

            if (!Ready || _winsorDeviceCache is null)
                throw new ServiceNotReadyException(_logging, "Device Cache has not been populated yet.");

            return [.. _winsorDeviceCache];
        }
    }

    private ImmutableArray<DeviceCategoryRecord> _categories = [];

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

            return _categories;
        }
    }

    public bool Started { get; private set; }

    public double Progress { get; private set; } = 0;

    public string CacheFileName => ".devices.cache";

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
        await ReloadCache(onError);
        SaveCache();
    }

    public async Task Initialize(ErrorAction onError)
    {
        if (Started)
            return;

        Started = true;

        if (!LoadCache())
        {
            await ReloadCache(onError);
            SaveCache();
        }
        Progress = 1;
        Ready = true;
    }

    private async Task ReloadCache(ErrorAction onError)
    {
        var loanersTask = _api.SendAsync<List<DeviceRecord>>(HttpMethod.Get,
                        "api/devices/loaners", onError: onError)!;
        loanersTask.WhenCompleted(() =>
        {
            Progress += 1 / 4.0;
            _loaners = loanersTask.Result!;
        });

        var devicesTask = _api.SendAsync<List<DeviceRecord>>(HttpMethod.Get,
            "api/devices?isActive=true", onError: onError);

        devicesTask.WhenCompleted(() =>
        {
            Progress += 1 / 4.0;
            _deviceCache = devicesTask.Result ?? [];
        });



        var categoriesTask = _api.SendAsync<ImmutableArray<DeviceCategoryRecord>>(HttpMethod.Get,
            "api/devices/categories", onError: onError);
        categoriesTask.WhenCompleted(() =>
        {
            Progress += 1 / 4.0;
            _categories = categoriesTask.Result;
        });


        var winsorDeviceCache = _api.SendAsync<List<WinsorDeviceRecord>>(HttpMethod.Get, "api/devices/winsor-devices?isActive=true", onError: onError);


        winsorDeviceCache.WhenCompleted(() =>
        {
            _winsorDeviceCache = winsorDeviceCache.Result ?? [];
            Progress += 0.25;
        });

        await Task.WhenAll(loanersTask, devicesTask, categoriesTask, winsorDeviceCache);
    }

    public async Task<DeviceRecord?> UpdateDevice(string deviceId, UpdateDeviceRecord update, ErrorAction onError)
    {
        var result = await _api.SendAsync<UpdateDeviceRecord, DeviceRecord?>(HttpMethod.Put,
            $"api/devices/{deviceId}", update, onError: onError);


        if (result.HasValue && DeviceCache.Any(dev => dev.id == result.Value.id))
            _deviceCache!.Replace(DeviceCache.First(dev => dev.id == result.Value.id), result.Value);
        else if (result.HasValue)
        {
            _deviceCache!.Add(result.Value);
            if (result.Value.winsorDevice.HasValue)
            {
                var newDetails = await GetWinsorDeviceDetails(deviceId, onError);
                if (newDetails.HasValue)
                {
                    if (_winsorDeviceCache.Any(dev => dev.assetTag == result.Value.winsorDevice.Value.assetTag))
                    {
                        var rep = _winsorDeviceCache.First(dev => dev.assetTag == result.Value.winsorDevice.Value.assetTag);
                        _winsorDeviceCache.Replace(rep, newDetails.Value);
                    }
                    else
                        _winsorDeviceCache.Add(newDetails.Value);
                }
            }
        }

        SaveCache();
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

        SaveCache();
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

        SaveCache();
        return results.First();
    }

    public async Task<WinsorDeviceRecord?> GetWinsorDeviceDetails(string id, ErrorAction? onError = null)
    {
        if (string.IsNullOrEmpty(id)) 
            return null;

        if (WinsorDeviceCache.Any(dev => dev.id == id))
            return WinsorDeviceCache.First(dev => dev.id == id);

        var result = await _api.SendAsync<WinsorDeviceRecord?>(HttpMethod.Get,
            $"api/devices/{id}/winsor-device-info", onError: onError);
        if (result.HasValue)
            _winsorDeviceCache?.Add(result.Value);
        SaveCache();
        return result;
    }

    public async Task<bool> DisposeDevice(string id, ErrorAction onError)
    {

        var result = await _api.SendAsync<DeviceRecord?>(HttpMethod.Delete,
            $"api/devices/{id}/dispose-inventory", onError: onError);
        if (!result.HasValue) return false;

        var dev = DeviceCache.First(d => d.id == id);
        _deviceCache!.Replace(dev, result.Value);
        SaveCache();
        return true;
    }

    private readonly record struct CacheSchema(
        ImmutableArray<DeviceRecord> devices, 
        ImmutableArray<WinsorDeviceRecord> winsorDevices,
        ImmutableArray<DeviceRecord> loaners,
        ImmutableArray<DeviceCategoryRecord> categories
    );

    public void SaveCache()
    {
        var cacheFilePath = $"{_logging.AppStoragePath}{Path.DirectorySeparatorChar}{CacheFileName}";
        var cache = new CacheSchema([.. _deviceCache], [.._winsorDeviceCache], [.._loaners], [.._categories]);
        var json = JsonSerializer.Serialize(cache);
        File.WriteAllText(cacheFilePath, json);
    }

    public bool LoadCache()
    {
        var cacheFilePath = $"{_logging.AppStoragePath}{Path.DirectorySeparatorChar}{CacheFileName}";
        if (!File.Exists(cacheFilePath))
            return false;

        var json = File.ReadAllText(cacheFilePath);

        try
        {
            var cache = JsonSerializer.Deserialize<CacheSchema>(json);
            _deviceCache = [..cache.devices];
            _winsorDeviceCache = [..cache.winsorDevices];
            _loaners = [..cache.loaners];
            _categories = [..cache.categories];
            return true;
        }
        catch 
        { 
            return false; 
        }
    }
}