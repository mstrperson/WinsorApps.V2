using System.Text.Json;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.Services.Helpdesk.Services;

public sealed class DeviceService(ApiService api, LocalLoggingService logging) : IAsyncInitService
{
    private readonly ApiService _api = api;
    private readonly LocalLoggingService _logging = logging;
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

    private List<DeviceCategoryRecord> _categories = [];

    public List<DeviceCategoryRecord> Categories
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
    public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }

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
        await SaveCache();
    }

    public async Task Initialize(ErrorAction onError)
    {
        if (Started)
            return;

        Started = true;

        if (!LoadCache())
        {
            await ReloadCache(onError);
            await SaveCache();
        }
        Progress = 1;
        Ready = true;
    }

    private async Task ReloadCache(ErrorAction onError)
    {
        _loaners = await _api.SendAsync<List<DeviceRecord>>(HttpMethod.Get,
                        "api/devices/loaners", onError: onError) ?? [];
        
        _deviceCache = await _api.SendAsync<List<DeviceRecord>>(HttpMethod.Get,
            "api/devices?isActive=true", onError: onError) ?? [];

        _categories = await _api.SendAsync<List<DeviceCategoryRecord>>(HttpMethod.Get,
            "api/devices/categories", onError: onError) ?? [];
        
        _winsorDeviceCache = await _api.SendAsync<List<WinsorDeviceRecord>>(HttpMethod.Get, 
            "api/devices/winsor-devices?isActive=true", onError: onError) ?? [];
        
    }

    public async Task<DeviceRecord?> UpdateDevice(string deviceId, UpdateDeviceRecord update, ErrorAction onError)
    {
        var result = await _api.SendAsync<UpdateDeviceRecord, DeviceRecord?>(HttpMethod.Put,
            $"api/devices/{deviceId}", update, onError: onError);


        if (result is not null && DeviceCache.Any(dev => dev.id == result.id))
            _deviceCache!.Replace(DeviceCache.First(dev => dev.id == result.id), result);
        else if (result is not null)
        {
            _deviceCache!.Add(result);
            if (result.winsorDevice is not null)
            {
                var newDetails = await GetWinsorDeviceDetails(deviceId, onError);
                if (newDetails is not null)
                {
                    if (_winsorDeviceCache.Any(dev => dev.assetTag == result.winsorDevice.assetTag))
                    {
                        var rep = _winsorDeviceCache.First(dev => dev.assetTag == result.winsorDevice.assetTag);
                        _winsorDeviceCache.Replace(rep, newDetails);
                    }
                    else
                        _winsorDeviceCache.Add(newDetails);
                }
            }
        }

        await SaveCache();
        return result;
    }

    public async Task<DeviceRecord?> CreateNewDeviceRecord(CreateDeviceRecord newDevice, ErrorAction onError)
    {
        DeviceRecord? temp = null;
        bool wait = false;
        var result = await _api.SendAsync<CreateDeviceRecord, DeviceRecord?>(HttpMethod.Post, "api/devices", newDevice,
            onError: async (err) =>
            {
                wait = true;
                var dev = await SearchDevices(newDevice.serialNumber, onError);
                if(dev is not null)
                {
                    temp = dev;
                    wait = false;
                    return;
                }
                wait = false;
                onError(err);
            });
        while (wait)
            await Task.Delay(250);
        if(temp is not null)
            result = temp;

        if (result is not null && DeviceCache.Any(dev => dev.id == result.id))
            _deviceCache!.Replace(DeviceCache.First(dev => dev.id == result.id), result);
        else if (result is not null)
            _deviceCache!.Add(result);

        await SaveCache();
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
            dev.serialNumber.Equals(identifier, StringComparison.InvariantCultureIgnoreCase) ||
            (dev.winsorDevice?.assetTag.ToUpperInvariant() ?? string.Empty) == identifier))
        {
            return DeviceCache.First(dev =>
                dev.serialNumber.Equals(identifier, StringComparison.InvariantCultureIgnoreCase) ||
                (dev.winsorDevice?.assetTag.ToUpperInvariant() ?? string.Empty) == identifier)!;
        }

        var results = await _api.SendAsync<List<DeviceRecord>>(HttpMethod.Get,
                $"api/devices?serialNumberPattern={identifier}", onError: onError) ?? [];
        if (results.Count == 0)
            results = await _api.SendAsync<List<DeviceRecord>>(HttpMethod.Get,
                $"api/devices?assetTagPattern={identifier}", onError: onError) ?? [];

        if (results.Count == 0)
            return null;

        foreach (var result in results)
        {
            if (DeviceCache.All(dev => dev.id != result.id))
                _deviceCache!.Add(result);
        }

        await SaveCache();
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
        if (result is not null)
            _winsorDeviceCache.Add(result);
        await SaveCache();
        return result;
    }

    public async Task<bool> DisposeDevice(string id, ErrorAction onError)
    {

        var result = await _api.SendAsync<DeviceRecord?>(HttpMethod.Delete,
            $"api/devices/{id}/dispose-inventory", onError: onError);
        if (result is null) return false;

        var dev = DeviceCache.First(d => d.id == id);
        _deviceCache!.Replace(dev, result);
        await SaveCache();
        return true;
    }

    private record CacheSchema(
        List<DeviceRecord> devices, 
        List<WinsorDeviceRecord> winsorDevices,
        List<DeviceRecord> loaners,
        List<DeviceCategoryRecord> categories
    );

    public async Task SaveCache()
    {
        var cacheFilePath = $"{_logging.AppStoragePath}{Path.DirectorySeparatorChar}{CacheFileName}";
        var cache = new CacheSchema([.. _deviceCache], [.._winsorDeviceCache], [.._loaners], [.._categories]);
        var json = JsonSerializer.Serialize(cache);
        await File.WriteAllTextAsync(cacheFilePath, json);
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
            if(cache is null) return false;
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