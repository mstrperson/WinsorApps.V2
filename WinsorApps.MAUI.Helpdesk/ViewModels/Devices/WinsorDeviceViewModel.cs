using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Global;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels;

public partial class WinsorDeviceViewModel : ObservableObject
{
    private readonly DeviceService _deviceService;
    
    private readonly WinsorDeviceStub _device;

    [ObservableProperty] private bool hasWinsorData;
    [ObservableProperty] private string assetTag;
    [ObservableProperty] private string categoryName;
    [ObservableProperty] private string cheqroomId;
    [ObservableProperty] private int jamfId;
    [ObservableProperty] private int jamfInventoryPreloadId;
    [ObservableProperty] private bool loaner;
    [ObservableProperty] private DeviceCategoryViewModel category;
    [ObservableProperty] private DateTime purchaseDate;
    [ObservableProperty] private double purchaseCost;

    public WinsorDeviceViewModel()
    {
        _deviceService = ServiceHelper.GetService<DeviceService>()!;
        category = new(_deviceService.Categories.First());
        hasWinsorData = false;
        _device = new();
        assetTag = _device.assetTag;
        categoryName = _device.category;
        cheqroomId = _device.cheqroomId;
        jamfId = _device.jamfId;
        jamfInventoryPreloadId = _device.jamfInventoryPreloadId;
        loaner = _device.loaner;
    }
    
    public WinsorDeviceViewModel(DeviceRecord dev)
    {
        _deviceService = ServiceHelper.GetService<DeviceService>()!;
        hasWinsorData = dev.isWinsorDevice;
        _device = dev.winsorDevice ?? new();
        assetTag = _device.assetTag;
        categoryName = _device.category;
        cheqroomId = _device.cheqroomId;
        jamfId = _device.jamfId;
        jamfInventoryPreloadId = _device.jamfInventoryPreloadId;
        loaner = _device.loaner;
        category = new(_device.category);
        var task = _deviceService.GetWinsorDeviceDetails(dev.id);
        task.WhenCompleted(() =>
        {
            var details = task.Result!.Value;
            PurchaseDate = details.purchaseDate;
            PurchaseCost = details.purchaseCost;
        });
    }

    public UpdateWinsorDeviceRecord GetUpdateRecord() =>
        new(Category.Id, PurchaseDate, PurchaseCost);

    public CreateWinsorDeviceRecord GetCreateRecord() =>
        new(Category.Id, PurchaseDate, PurchaseCost);
}