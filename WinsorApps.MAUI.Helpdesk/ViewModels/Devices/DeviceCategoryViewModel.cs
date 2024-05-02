using CommunityToolkit.Mvvm.ComponentModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels;

public class DeviceCategoryViewModel
{
    private readonly DeviceCategoryRecord _category;

    public string Id => _category.id;
    
    [ObservableProperty] private string name;
    [ObservableProperty] private string prefix;
    [ObservableProperty] private bool assignCustody;
    [ObservableProperty] private string cheqroomCategory;
    [ObservableProperty] private string cheqroomLocation;
    [ObservableProperty] private string jamfDepartment;
    [ObservableProperty] private string jamfDeviceType;

    public DeviceCategoryViewModel(DeviceCategoryRecord category)
    {
        _category = category;
        name = category.name;
        assignCustody = category.assignCustodyToOwner;
        prefix = category.assetTagPrefix;
        cheqroomCategory = category.cheqroomCategory;
        cheqroomLocation = category.cheqroomLocation;
        jamfDepartment = category.jamfDepartment;
        jamfDeviceType = category.jamfDeviceType;
    }

    public DeviceCategoryViewModel(string name)
    {
        var service = ServiceHelper.GetService<DeviceService>()!;
        var category =
            service.Categories.FirstOrDefault(cat =>
                cat.name.Contains(name, StringComparison.InvariantCultureIgnoreCase));
        
        _category = category;
        name = category.name;
        assignCustody = category.assignCustodyToOwner;
        prefix = category.assetTagPrefix;
        cheqroomCategory = category.cheqroomCategory;
        cheqroomLocation = category.cheqroomLocation;
        jamfDepartment = category.jamfDepartment;
        jamfDeviceType = category.jamfDeviceType;
    }

}