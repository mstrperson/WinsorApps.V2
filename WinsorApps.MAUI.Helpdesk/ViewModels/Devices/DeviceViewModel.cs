using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels;

public partial class DeviceViewModel : ObservableObject
{
    private readonly DeviceService _deviceService;
    private DeviceRecord _device;

    [ObservableProperty] private string id;
    [ObservableProperty] private string serialNumber;
    [ObservableProperty] private UserViewModel owner;
    [ObservableProperty] private bool unicorn;
    [ObservableProperty] private DateTime firstSeen;
    [ObservableProperty] private bool isActive;
    [ObservableProperty] private string type;
    [ObservableProperty] private bool isWinsorDevice;
    [ObservableProperty] private WinsorDeviceViewModel winsorDevice;

    public event EventHandler<ErrorRecord>? OnError;

    public DeviceViewModel()
    {
        _deviceService = ServiceHelper.GetService<DeviceService>()!;
        _device = new();
        id = "";
        type = "";
        serialNumber = "";
        owner = UserViewModel.Empty;
        winsorDevice = new();
    }

    public DeviceViewModel(DeviceRecord device)
    {
        _deviceService = ServiceHelper.GetService<DeviceService>()!;
        _device = device;
        id = device.id;
        serialNumber = device.serialNumber;
        owner = device.owner.HasValue ? 
            new UserViewModel(device.owner!.Value) 
            : UserViewModel.Empty;
        unicorn = device.unicorn;
        firstSeen = device.firstSeen;
        isActive = device.isActive;
        type = device.type;
        isWinsorDevice = device.isWinsorDevice;
        WinsorDevice = new(device);
    }

    public CreateDeviceRecord GetCreateRecord(CreateWinsorDeviceRecord? winsorDevice = null) =>
        new(SerialNumber, Type, string.IsNullOrEmpty(Owner.Id) ? null : Owner.Id, Unicorn,
            IsActive, winsorDevice);

    public UpdateDeviceRecord GetUpdateRecord(UpdateWinsorDeviceRecord? winsorDevice = null) =>
        new(string.IsNullOrEmpty(Owner.Id) ? null : Owner.Id, Type, Unicorn, IsActive, winsorDevice);

    [RelayCommand]
    public async Task Save()
    {
        if (string.IsNullOrEmpty(Id))
        {
            var newDev = GetCreateRecord(IsWinsorDevice ? WinsorDevice.GetCreateRecord() : null);
            var result = await _deviceService
                .CreateNewDeviceRecord(newDev, OnErr);
            if (!result.HasValue)
                return;

            _device = result.Value;
            Id = _device.id;
        }
        else
        {
            var update = GetUpdateRecord(IsWinsorDevice ? WinsorDevice.GetUpdateRecord() : null);
            var updateResult = await _deviceService
                .UpdateDevice(Id, update, OnErr);

            if (!updateResult.HasValue)
                return;

            _device = updateResult.Value;
        }

        if (_device.isWinsorDevice)
        {
            WinsorDevice = new(_device);
        }
    }

    [RelayCommand]
    public async Task Dispose()
    {
        var result = await _deviceService.DisposeDevice(Id, OnErr);
        if (result)
        {
            _device = _deviceService.DeviceCache.First(dev => dev.id == Id);
            IsActive = false;
            WinsorDevice = new(_device);
        }
    }
    
    private void OnErr(ErrorRecord err)
    {
        OnError?.Invoke(this, err);
    }
}