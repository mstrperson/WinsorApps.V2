using System.Collections.Immutable;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Models;
public record ApprovalStatus(string id, string label)
{
    public static implicit operator KeyValuePair<string, string>(ApprovalStatus status) => new(status.label, status.id);
    public static implicit operator ApprovalStatus(KeyValuePair<string, string> status) => new(status.Value, status.Key);
}

public record ApprovalStatusLabel
{
    public static readonly ApprovalStatusLabel Pending = new("Pending");
    public static readonly ApprovalStatusLabel Approved = new("Approved");
    public static readonly ApprovalStatusLabel Withdrawn = new("Withdrawn");
    public static readonly ApprovalStatusLabel Declined = new("Declined");
    public static readonly ApprovalStatusLabel RoomNotCleared = new("Room Not Cleared");
    public static readonly ApprovalStatusLabel Draft = new("Draft");
    public static readonly ApprovalStatusLabel Creating = new("Creating");
    public static readonly ApprovalStatusLabel Updating = new("Updating");
    public static readonly ApprovalStatusLabel Empty = new("Unknown");

    public static implicit operator string(ApprovalStatusLabel label) => label._label;
    public static implicit operator ApprovalStatusLabel(string label) => label.ToLowerInvariant() switch
    {
        "pending" => Pending,
        "approved" => Approved,
        "withdrawn" => Withdrawn,
        "declined" => Declined,
        "room not cleared" => RoomNotCleared,
        "draft" => Draft,
        "creating" => Creating,
        "updating" => Updating,
        "unknown" or "" => Empty,
        _ => throw new InvalidCastException($"{label} is not a valid Approval Status Label.")
    };

    private readonly string _label;

    private ApprovalStatusLabel(string label)
    {
        this._label = label;
    }
}

public record EventType
{
    public static readonly EventType Default = new("Default");
    public static readonly EventType Rental = new("Rental");
    public static readonly EventType FieldTrip = new("Field Trip");
    public static readonly EventType VirtualEvent = new("Virtual Event");

    public static implicit operator string(EventType type) => type._type;
    public static implicit operator EventType(string str) =>
        str.ToLowerInvariant().Replace('-', ' ').Trim() switch
        {
            "default" => Default,
            "rental" => Rental,
            "field trip" => FieldTrip,
            "virtual event" => VirtualEvent,
            _ => throw new InvalidCastException($"{str} is not a valid event type.")
        };

    private readonly string _type;

    private EventType(string type) => _type = type;
}

public record Location(string id, string label, string type)
{
    public static readonly Location None = new("", "None", "On Campus");
}


public record CalendarEvent<T>(DateTime start, DateTime end, string summary, T details, UserRecord eventCreator, List<Location> locations);

/// <summary>
/// Required details for creating a new event.
/// </summary>
/// <param name="summary">Title for the Event (single-line)</param>
/// <param name="description">Details about the event (multi-line)</param>
/// <param name="type">event type name</param>
/// <param name="start">start date and time of this event</param>
/// <param name="end">end date and time</param>
/// <param name="creatorId">id hash of the creator</param>
/// <param name="leaderId">id hash of the event leader</param>
/// <param name="preaprovalDate">date that this form was approved (before you entered it...)</param>
/// <param name="attendeeCount">estimated number of people</param>
/// <param name="managerId">not used, but if we do.  who is the manager?</param>
/// <param name="selectedLocations">list of hashed ids for the selected locations</param>
/// <param name="selectedCustomLocations">list of hashed ids for selected custom locations</param>
public record NewEvent(string summary, string description, string type, DateTime start, DateTime end,
        string creatorId, string leaderId, DateOnly preaprovalDate, int attendeeCount, string? managerId = null,
        List<string>? selectedLocations = null, List<string>? selectedCustomLocations = null);


public record UserInfoShort(string id, string name, string email)
{
    public static implicit operator UserInfoShort(UserRecord user) => new(user.id, $"{user.nickname} {user.lastName}", user.email);
}

public record EventBaseShortDetails(string id, string summary, string description, string type, string status,
    DateTime start, DateTime end, UserInfoShort creator, UserInfoShort leader);

public record EventFormBase(string id, string summary, string description, string type, string status, DateTime start, DateTime end,
        string creatorId, string leaderId, DateOnly preaprovalDate, int attendeeCount, string? managerId = null,
        List<string>? selectedLocations = null, List<string>? selectedCustomLocations = null, List<DocumentHeader>? attachments = null,
        bool hasFacilitiesInfo = false, bool hasTechRequest = false, bool hasCatering = false, bool hasTheaterRequest = false, bool hasFieldTripInfo = false,
        bool hasZoom = false, bool hasMarCom = false)
{
    public bool IsSameAs(EventFormBase b)
    {
        if(id != b.id) 
            return false;
        using DebugTimer _ = new($"Checking diff of event {id}");
        var result = summary.Equals(b.summary, StringComparison.InvariantCultureIgnoreCase)
            && description.Equals(b.description, StringComparison.InvariantCultureIgnoreCase)
            && type.Equals(b.type, StringComparison.InvariantCultureIgnoreCase)
            && status.Equals(b.status, StringComparison.InvariantCultureIgnoreCase)
            && start == b.start
            && attendeeCount == b.attendeeCount
            && (selectedLocations ?? []).SequenceEqual(b.selectedLocations ?? [])
            && (selectedCustomLocations ?? []).SequenceEqual(b.selectedCustomLocations ?? [])
            && hasFacilitiesInfo == b.hasFacilitiesInfo
            && hasCatering == b.hasCatering
            && hasFieldTripInfo == b.hasFieldTripInfo
            && hasTechRequest == b.hasTechRequest
            && hasZoom == b.hasZoom
            && hasMarCom == b.hasMarCom
            && hasTheaterRequest == b.hasTheaterRequest;

        return result;
    }


    public static EventFormBase Empty => new("", "", "", "", "", default, default, "", "", default, 0, "", [], [], [], false, false, false, false, false, false, false);
}

public record EventBaseUpdate(int id, string summary, string description, string type, string status, DateTime start, DateTime end,
         int leaderId, DateOnly preaprovalDate, int attendeeCount, int? managerId = null,
        List<string>? locationsAdded = null, List<string>? locationsRemoved = null,
        List<string>? customLocationsAdded = null, List<string>? customLocationsRemoved = null);
