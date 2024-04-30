namespace WinsorApps.Services.Helpdesk.Models;

public static partial class Computer
    {
        public readonly record struct Hardware(
            string make, string model, string serialNumber, string macAddress, string altMacAddress);
        public readonly record struct UserAndLocation(
            string username, string realname, string email, string departmentId);

        public readonly record struct GeneralDetail(
            string name, string lastIpAddress, string lastReportedIp, string assetTag,
            DateTime? reportDate, DateTime? lastContactTime, DateTime? initialEntryDate);

        public readonly record struct LocalAccount(
            string uid, string username, string fullName, bool admin, string userAccountType);

        public readonly record struct Details(string id, string udid,
            GeneralDetail general = default, ImmutableArray<LocalAccount> localUserAccounts = default,
            UserAndLocation userAndLocation = default, Hardware hardware = default);
    }

    public static partial class MobileDevice
    {
        public readonly record struct Info(string model, string modelNumber);

        public readonly record struct UserAndLocation(
            string username, string realName, string emailAddress, string departmentId, string room);

        public readonly record struct Details(
            string id, string name, string assetTag, string serialNumber, string wifiMacAddress,
            DateTime? initialEntryTimestamp, string type, DateTime? lastInventoryUpdateTimestamp,
            UserAndLocation location, Info? ios = null, Info? tvos = null);
    }

    public readonly record struct Department(string id, string name)
    {
        public static implicit operator string(Department dept) => dept.name;

        // Todo: Move to Service.
        /*public static implicit operator Department(string name)
        {
            if (!JamfService.Departments.Any(dept => dept.name == name))
                throw new InvalidCastException($"{name} is not a valid Department");

            return JamfService.Departments.First(dept => dept.name == name);
        }*/

        public override string ToString() => name;
    }

    public sealed class DeviceType
    {
        public static DeviceType Computer = new("Computer");
        public static DeviceType MobileDevice = new("Mobile Device");

        private string _type;

        public string ApiEndpoint => _type.ToLowerInvariant().Replace(' ', '-');
        public Type DeviceDataType => _type switch
        {
            "Computer" => typeof(Computer.Details),
            "Mobile Device" => typeof(MobileDevice.Details),
            _ => throw new InvalidDataException("Invalid Jamf Device Type")
        };

        private DeviceType(string type)
        {
            if (type != "Computer" && type != "Mobile Device")
                throw new InvalidCastException("Type must be either \"Computer\" or \"Mobile Device\"");

            _type = type;
        }

        public override string ToString() => _type;

        public static implicit operator DeviceType(string type) => type switch
        {
            "Computer" => Computer,
            "Mobile Device" => MobileDevice,
            _ => throw new InvalidCastException("Type must be either \"Computer\" or \"Mobile Device\"")
        };

        public static implicit operator string(DeviceType type) => type._type;
    }

    public readonly record struct InventoryPreloadEntry(string id, string serialNumber, string username, string fullName,
        string emailAddress, string phoneNumber, string position, string department, string building, 
        string room, string assetTag, string deviceType);