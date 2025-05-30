using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.Services.Helpdesk.Services;

public class JamfService(ApiService api, LocalLoggingService logging) : IAsyncInitService
{
    private readonly ApiService _api = api;
    private readonly LocalLoggingService _logging = logging;

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

    public bool Ready { get; private set; } = false;

    public List<Department> Departments { get; private set; } = [];

    public bool Started { get; private set; }
    public double Progress { get; private set; } = 0;

    public string CacheFileName => "jamf.cache.json";
    public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }

    public async Task Refresh(ErrorAction onError)
    {
        Started = false;
        Progress = 0;
        await Initialize(onError);
    }

    public async Task Initialize(ErrorAction onError)
    {
        if (Started) return;

        Started = true;
        Departments = await _api.SendAsync<List<Department>>(HttpMethod.Get,
            "api/devices/jamf/departments", onError: onError) ?? [];
        Progress = 1;
        Ready = true;
    }

    public async Task<Computer.Details?> GetComputerDetails(string deviceId, ErrorAction onError)
    {


        var result = await _api.SendAsync<Computer.Details?>(HttpMethod.Get,
            $"api/devices/{deviceId}/jamf/computer", onError: onError);

        return result;
    }

    public async Task<MobileDevice.Details?> GetMobileDeviceDetails(string deviceId, ErrorAction onError)
    {

        var result = await _api.SendAsync<MobileDevice.Details?>(HttpMethod.Get,
            $"api/devices/{deviceId}/jamf/mobile-device", onError: onError);

        return result;
    }

    public async Task<InventoryPreloadEntry?> GetInventoryPreload(string deviceId, ErrorAction onError)
    {

        var result = await _api.SendAsync<InventoryPreloadEntry?>(HttpMethod.Get,
            $"api/devices/{deviceId}/jamf/inventory-preload", onError: onError);

        return result;
    }

    public Department GetDepartmentByName(string name) => Departments.First(dept => dept.name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

    public async Task SaveCache()
    {
        throw new NotImplementedException();
    }

    public bool LoadCache()
    {
        throw new NotImplementedException();
    }
}