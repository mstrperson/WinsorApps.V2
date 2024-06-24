using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinsorApps.Services.EventForms.Models;

public readonly record struct VehicleCategory(string id, string label, int passengers);

public readonly record struct VehicleRequest(string categoryId, string category, int numberNeeded, string notes);

public readonly record struct FieldTripHiredBusRequest(int busCount, TimeOnly departureTime, TimeOnly busPickupTime, TimeOnly returnTime, string instructions);

public readonly record struct NewFieldTripCateringRequest(int numberOfLunches, ImmutableArray<string>? menuItemIds, int diningCount, bool eatingAway, TimeOnly pickupTime);

public readonly record struct FieldTripCateringRequest(int numberOfLunches, ImmutableArray<DetailedCateringMenuSelection> menuItems, int diningInCount,
    bool eatingAway, TimeOnly pickupTime);

public readonly record struct NewVehicleRequest(string categoryId, int number, string notes = "");

public readonly record struct NewTransportationRequest(bool usePublicTransit = false, bool noOrganizedTransit = false,
    ImmutableArray<NewVehicleRequest>? vehicleRequests = null, FieldTripHiredBusRequest? hiredBusses = null);

public readonly record struct TransportationRequest(bool usePublicTransit = false, bool noOrganizedTransit = false,
    bool hasVehicleRequest = false, bool hasBusRequest = false,
    ImmutableArray<VehicleRequest>? vehicleRequests = null,
    FieldTripHiredBusRequest? hiredBusses = null);

public readonly record struct StudentsByClassCount(int classI = 0, int classII = 0, int classIII = 0, int classIV = 0,
                                    int classV = 0, int classVI = 0, int classVII = 0, int classVIII = 0);

public readonly record struct NewFieldTrip(string primaryContactId, NewTransportationRequest? transportationDetails,
    StudentsByClassCount? studentCount, ImmutableArray<string>? chaparoneIds, NewFieldTripCateringRequest? lunch = null);

public readonly record struct FieldTrip(Contact primaryContact, StudentsByClassCount studentCount,
    ImmutableArray<Contact> chaperones,
    ImmutableArray<string> chaparoneIds, bool hasLunch);

public readonly record struct FieldTripDetails(string eventId, Contact primaryContact,
    TransportationRequest transportationDetails,
    StudentsByClassCount studentCount,
    ImmutableArray<Contact> chaperones,
    ImmutableArray<string> chaparoneIds,
    FieldTripCateringRequest? lunch = null);