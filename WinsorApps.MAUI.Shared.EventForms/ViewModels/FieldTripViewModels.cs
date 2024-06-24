using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.EventForms.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class TransportationViewModel :
    ObservableObject
{
    [ObservableProperty] bool publicTransit;
    [ObservableProperty] bool noOrganizedTransit;
    [ObservableProperty] VehicleRequestCollectionViewModel vehicleRequestCollection = new();
    [ObservableProperty] HiredBusViewModel hiredBusses = new();

    public static implicit operator NewTransportationRequest(TransportationViewModel vm) =>
        new(vm.PublicTransit, vm.NoOrganizedTransit,
            vm.VehicleRequestCollection.Requests.Any() ?
                vm.VehicleRequestCollection.Requests.Select(req => (NewVehicleRequest)req).ToImmutableArray() : null,
            vm.HiredBusses.Count > 0 ?
                (FieldTripHiredBusRequest)vm.HiredBusses : null);

    public static TransportationViewModel Get(TransportationRequest model)
    {
        var vm = new TransportationViewModel()
        {
            PublicTransit = model.usePublicTransit,
            NoOrganizedTransit = model.noOrganizedTransit
        };
        if (model.hiredBusses.HasValue)
            vm.HiredBusses = HiredBusViewModel.Get(model.hiredBusses.Value);

        if (model.vehicleRequests.HasValue)
            vm.VehicleRequestCollection.LoadRequests(model.vehicleRequests.Value.Select(VehicleRequestViewModel.Get));

        return vm;
    }

}