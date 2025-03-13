namespace WinsorApps.Services.Helpdesk.Models;

public static partial class Computer
{
    public record Hardware(
        string make, string model, string serialNumber, string macAddress, string altMacAddress);
    public record UserAndLocation(
        string username, string realname, string email, string departmentId);

    public record GeneralDetail(
        string name, string lastIpAddress, string lastReportedIp, string assetTag,
        DateTime? reportDate, DateTime? lastContactTime, DateTime? initialEntryDate);

    public record LocalAccount(
        string uid, string username, string fullName, bool admin, string userAccountType);

    public record Details(string id, string udid,
        GeneralDetail? general, List<LocalAccount> localUserAccounts,
        UserAndLocation? userAndLocation, Hardware? hardware)
    {
        public static JamfDeviceType DeviceType => JamfDeviceType.Computer;
    }
}

public static partial class MobileDevice
{
    public record Info(string model, string modelNumber);

    public record UserAndLocation(
        string username, string realName, string emailAddress, string departmentId, string room);

    public record Details(
        string id, string name, string assetTag, string serialNumber, string wifiMacAddress,
        DateTime? initialEntryTimestamp, string type, DateTime? lastInventoryUpdateTimestamp,
        UserAndLocation location, Info? ios = null, Info? tvos = null)
    {
        public static JamfDeviceType DeviceType => JamfDeviceType.MobileDevice;
    }
}

public record Department(string id, string name)
{
    public static implicit operator string(Department dept) => dept.name;
    public override string ToString() => name;
}

public sealed class JamfDeviceType
{
    public static readonly JamfDeviceType Computer = new("Computer");
    public static readonly JamfDeviceType MobileDevice = new("Mobile Device");

    private readonly string _type;

    public string ApiEndpoint => _type.ToLowerInvariant().Replace(' ', '-');
    public Type DeviceDataType => _type switch
    {
        "Computer" => typeof(Computer.Details),
        "Mobile Device" => typeof(MobileDevice.Details),
        _ => throw new InvalidDataException("Invalid Jamf Device Type")
    };

    private JamfDeviceType(string type)
    {
        if (type != "Computer" && type != "Mobile Device")
            throw new InvalidCastException("Type must be either \"Computer\" or \"Mobile Device\"");

        _type = type;
    }

    public override string ToString() => _type;

    public static implicit operator JamfDeviceType(string type) => type switch
    {
        "Computer" => Computer,
        "Mobile Device" => MobileDevice,
        _ => throw new InvalidCastException("Type must be either \"Computer\" or \"Mobile Device\"")
    };

    public static implicit operator string(JamfDeviceType type) => type._type;
}

public record InventoryPreloadEntry(string id, string serialNumber, string username, string fullName,
    string emailAddress, string phoneNumber, string position, string department, string building, 
    string room, string assetTag, string deviceType);