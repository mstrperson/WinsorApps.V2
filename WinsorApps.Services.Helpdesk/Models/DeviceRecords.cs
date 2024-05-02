using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Helpdesk.Models;

public readonly record struct DeviceAssignmentRecord(string assetTag, UserRecord owner, DateTime dateAssigned, DateTime dateReleased, bool isCurrent);

    public readonly record struct DeviceCategoryRecord(string id, string name, string assetTagPrefix, bool assignCustodyToOwner,
        string cheqroomCategory, string cheqroomLocation, string jamfDepartment, string jamfDeviceType)
    {
        public override string ToString() => name;

        // todo: this goes in the Service!
        /*public static implicit operator DeviceCategoryRecord?(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return null;

            if (!DeviceService.Ready)
                throw new ServiceNotReadyException("Device Service is not ready to look up Category by string.");

            if (DeviceService.Categories.ToImmutableList()
                .TrueForAll(cat => cat.id != str && cat.name.ToLowerInvariant() != str.ToLowerInvariant()))
                return null;

            return DeviceService.Categories
                .First(cat => cat.id == str || cat.name.ToLowerInvariant() == str.ToLowerInvariant());
        }*/
    }
    public readonly record struct WinsorDeviceRecord(string id, string assetTag, DeviceCategoryRecord category, string cheqroomId,
        int jamfId, int jamfInventoryPreloadId,
        bool loaner, DateTime purchaseDate, double purchaseCost = 0.0);
    public readonly record struct WinsorDeviceStub(
        string assetTag, string category, string cheqroomId,
        int jamfId, int jamfInventoryPreloadId, bool loaner)
    {
        public static implicit operator WinsorDeviceStub?(WinsorDeviceRecord? dev) => dev.HasValue ? null :
            new(dev.Value.assetTag, dev.Value.category.name, 
                dev.Value.cheqroomId, dev.Value.jamfId, dev.Value.jamfInventoryPreloadId, dev.Value.loaner);
    }

    public readonly record struct DeviceRecord(string id, string serialNumber, UserRecord? owner, bool unicorn, DateTime firstSeen,
        bool isActive, string type, bool isWinsorDevice = false, WinsorDeviceStub? winsorDevice = null);

    public readonly record struct CreateWinsorDeviceRecord(string categoryId, DateTime purchaseDate = default, double value = 0);

    public readonly record struct CreateDeviceRecord(string serialNumber, string type, string? ownerId = null, bool unicorn = false,
        bool isActive = true, CreateWinsorDeviceRecord? winsorDeviceInfo = null);

    public readonly record struct UpdateDeviceRecord(string? ownerId = null, string? type = null, bool? unicorn = null,
        bool? active = null, UpdateWinsorDeviceRecord? winsorInfo = null);

    public readonly record struct UpdateWinsorDeviceRecord(string? categoryId = null, DateTime? purchaseDate = null,
        double? value = null);