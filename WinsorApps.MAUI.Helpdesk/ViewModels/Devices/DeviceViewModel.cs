using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Devices;

public partial class DeviceViewModel : ObservableObject, IEmptyViewModel<DeviceViewModel>, ISelectable<DeviceViewModel>, IErrorHandling
{
    protected readonly DeviceService _deviceService;
    protected DeviceRecord _device;

    [ObservableProperty] private string id;
    [ObservableProperty] private string serialNumber;
    [ObservableProperty] private UserSearchViewModel owner = new();
    [ObservableProperty] private bool unicorn;
    [ObservableProperty] private DateTime firstSeen;
    [ObservableProperty] private bool isActive;
    [ObservableProperty] private string type;
    [ObservableProperty] private bool isWinsorDevice;
    [ObservableProperty] private WinsorDeviceViewModel winsorDevice;
    [ObservableProperty] private bool isSelected;

    [ObservableProperty] private string displayName;

    public event EventHandler<ErrorRecord>? OnError;


    public event EventHandler<DeviceViewModel>? Selected;

    public DeviceViewModel()
    {
        _deviceService = ServiceHelper.GetService<DeviceService>()!;
        _device = new();
        displayName = "";
        id = "";
        type = "";
        serialNumber = "";
        winsorDevice = new();
    }

    public DeviceViewModel(DeviceRecord device)
    {
        _deviceService = ServiceHelper.GetService<DeviceService>()!;
        _device = device;
        displayName = device.serialNumber;
        id = device.id;
        serialNumber = device.serialNumber;
        if (device.owner.HasValue)
            Owner.Select(new(device.owner.Value));
        unicorn = device.unicorn;
        firstSeen = device.firstSeen;
        isActive = device.isActive;
        type = device.type;
        isWinsorDevice = device.isWinsorDevice;

        WinsorDevice = new(device);
        if (IsWinsorDevice)
            DisplayName = device.winsorDevice!.Value.assetTag;
    }

    public CreateDeviceRecord GetCreateRecord(CreateWinsorDeviceRecord? winsorDevice = null) =>
        new(SerialNumber, Type, string.IsNullOrEmpty(Owner.Selected.Id) ? null : Owner.Selected.Id, Unicorn,
            IsActive, winsorDevice);

    public UpdateDeviceRecord GetUpdateRecord(UpdateWinsorDeviceRecord? winsorDevice = null) =>
        new(string.IsNullOrEmpty(Owner.Selected.Id) ? null : Owner.Selected.Id, Type, Unicorn, IsActive, winsorDevice);

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
            DisplayName = _device.winsorDevice!.Value.assetTag;
        }
    }

    [RelayCommand]
    public async Task DisposeDevice()
    {
        var result = await _deviceService.DisposeDevice(Id, OnErr);
        if (result)
        {
            _device = _deviceService.DeviceCache.First(dev => dev.id == Id);
            IsActive = false;
            WinsorDevice = new(_device);
        }
    }

    [RelayCommand]
    public void Select() => Selected?.Invoke(this, this);
    
    private void OnErr(ErrorRecord err)
    {
        OnError?.Invoke(this, err);
    }
}