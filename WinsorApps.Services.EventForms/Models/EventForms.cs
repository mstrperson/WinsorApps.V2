using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Models;
public readonly record struct ApprovalStatus(string id, string label)
{
    public static implicit operator KeyValuePair<string, string>(ApprovalStatus status) => new(status.label, status.id);
    public static implicit operator ApprovalStatus(KeyValuePair<string, string> status) => new(status.Value, status.Key);
}

public readonly record struct Location(string id, string label, string type);


public readonly record struct CalendarEvent<T>(DateTime start, DateTime end, string summary, T details, UserRecord eventCreator, ImmutableArray<Location> locations);

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
public readonly record struct NewEvent(string summary, string description, string type, DateTime start, DateTime end,
        string creatorId, string leaderId, DateOnly preaprovalDate, int attendeeCount, string? managerId = null,
        ImmutableArray<string>? selectedLocations = null, ImmutableArray<string>? selectedCustomLocations = null);


public readonly record struct UserInfoShort(string id, string name, string email)
{
    public static implicit operator UserInfoShort(UserRecord user) => new(user.id, $"{user.nickname} {user.lastName}", user.email);
}

public readonly record struct EventBaseShortDetails(string id, string summary, string description, string type, string status,
    DateTime start, DateTime end, UserInfoShort creator, UserInfoShort leader);

public readonly record struct EventFormBase(string id, string summary, string description, string type, string status, DateTime start, DateTime end,
        string creatorId, string leaderId, DateOnly preaprovalDate, int attendeeCount, string? managerId = null,
        ImmutableArray<string>? selectedLocations = null, ImmutableArray<string>? selectedCustomLocations = null, ImmutableArray<DocumentHeader>? attachments = null,
        bool hasFacilitiesInfo = false, bool hasTechRequest = false, bool hasCatering = false, bool hasTheaterRequest = false, bool hasFieldTripInfo = false,
        bool hasZoom = false, bool hasMarCom = false);

public readonly record struct EventBaseUpdate(int id, string summary, string description, string type, string status, DateTime start, DateTime end,
         int leaderId, DateOnly preaprovalDate, int attendeeCount, int? managerId = null,
        ImmutableArray<string>? locationsAdded = null, ImmutableArray<string>? locationsRemoved = null,
        ImmutableArray<string>? customLocationsAdded = null, ImmutableArray<string>? customLocationsRemoved = null);
