using System.Collections.Immutable;

namespace WinsorApps.Services.EventForms.Models;

public record VehicleCategory(string id, string label, int passengers);

public record VehicleRequest(string categoryId, string category, int numberNeeded, string notes);

public record FieldTripHiredBusRequest(int busCount, TimeOnly departureTime, TimeOnly busPickupTime, TimeOnly returnTime, string instructions);

public record NewFieldTripCateringRequest(int numberOfLunches, List<string>? menuItemIds, int diningCount, bool eatingAway, TimeOnly pickupTime);

public record FieldTripCateringRequest(int numberOfLunches, List<DetailedCateringMenuSelection> menuItems, int diningInCount,
    bool eatingAway, TimeOnly pickupTime);

public record NewVehicleRequest(string categoryId, int number, string notes = "");

public record NewTransportationRequest(bool usePublicTransit = false, bool noOrganizedTransit = false,
    List<NewVehicleRequest>? vehicleRequests = null, FieldTripHiredBusRequest? hiredBusses = null);

public record TransportationRequest(bool usePublicTransit = false, bool noOrganizedTransit = false,
    bool hasVehicleRequest = false, bool hasBusRequest = false,
    List<VehicleRequest>? vehicleRequests = null,
    FieldTripHiredBusRequest? hiredBusses = null)
{
    public static readonly TransportationRequest Empty = new();
}

public record StudentsByClassCount(int classI = 0, int classII = 0, int classIII = 0, int classIV = 0,
                                    int classV = 0, int classVI = 0, int classVII = 0, int classVIII = 0)
{
    public static readonly StudentsByClassCount Empty = new();
}


public record NewFieldTrip(string primaryContactId, NewTransportationRequest? transportationDetails,
    StudentsByClassCount? studentCount, List<string>? chaparoneIds, NewFieldTripCateringRequest? lunch = null);

public record FieldTrip(Contact primaryContact, StudentsByClassCount studentCount,
    List<Contact> chaperones,
    List<string> chaparoneIds, bool hasLunch);

public record FieldTripDetails(string eventId, Contact primaryContact,
    TransportationRequest transportationDetails,
    StudentsByClassCount studentCount,
    List<Contact> chaperones,
    List<string> chaparoneIds,
    FieldTripCateringRequest? lunch = null)
{ 
    public static readonly FieldTripDetails Empty = new("", Contact.Empty, TransportationRequest.Empty, StudentsByClassCount.Empty, [], [], null);
}
