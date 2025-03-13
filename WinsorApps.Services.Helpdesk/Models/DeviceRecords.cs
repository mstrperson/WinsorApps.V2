using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Helpdesk.Models;

public record DeviceAssignmentRecord(string assetTag, UserRecord owner, DateTime dateAssigned, DateTime dateReleased, bool isCurrent);

public record DeviceCategoryRecord(string id, string name, string assetTagPrefix, bool assignCustodyToOwner,
    string cheqroomCategory, string cheqroomLocation, string jamfDepartment, string jamfDeviceType)
{
    public static DeviceCategoryRecord Empty => new("", "", "", false, "", "", "", "");

    public override string ToString() => name;
}
public record WinsorDeviceRecord(string id, string assetTag, DeviceCategoryRecord category, string cheqroomId,
    int jamfId, int jamfInventoryPreloadId,
    bool loaner, DateTime purchaseDate, double purchaseCost = 0.0)
{
    public static WinsorDeviceRecord Empty => new("", "", DeviceCategoryRecord.Empty, "", -1, -1, false, DateTime.Today);
}
public record WinsorDeviceStub(
    string assetTag, string category, string cheqroomId,
    int jamfId, int jamfInventoryPreloadId, bool loaner)
{
public static WinsorDeviceStub Default => new("", "", "", -1, -1, false);

    public static implicit operator WinsorDeviceStub?(WinsorDeviceRecord? dev) => dev is null ? null :
        new(dev.assetTag, dev.category.name, 
            dev.cheqroomId, dev.jamfId, dev.jamfInventoryPreloadId, dev.loaner);
}

public record DeviceRecord(string id, string serialNumber, UserRecord? owner, bool unicorn, DateTime firstSeen,
    bool isActive, string type, bool isWinsorDevice = false, WinsorDeviceStub? winsorDevice = null)
{
public static DeviceRecord Empty => new("", "", null, false, DateTime.Today, true, "", false, null);
}

public record CreateWinsorDeviceRecord(string categoryId, DateTime purchaseDate = default, double value = 0);

public record CreateDeviceRecord(string serialNumber, string type, string? ownerId = null, bool unicorn = false,
    bool isActive = true, CreateWinsorDeviceRecord? winsorDeviceInfo = null);

public record UpdateDeviceRecord(string? ownerId = null, string? type = null, bool? unicorn = null,
    bool? active = null, UpdateWinsorDeviceRecord? winsorInfo = null);

public record UpdateWinsorDeviceRecord(string? categoryId = null, DateTime? purchaseDate = null,
    double? value = null);