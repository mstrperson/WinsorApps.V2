using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class FieldTripViewModel :
    ObservableObject,
    IEventSubFormViewModel,
    IErrorHandling
{
    [ObservableProperty] string id = "";
    [ObservableProperty] ContactSearchViewModel primaryContactSearch = new() { SelectionMode = SelectionMode.Single };
    [ObservableProperty] ContactSearchViewModel chaperoneSearch = new() { SelectionMode = SelectionMode.Multiple };
    [ObservableProperty] TransportationViewModel transportation = new();
    [ObservableProperty] StudentsByClassViewModel studentsByClass = new();
    [ObservableProperty] FieldTripCateringRequestViewModel fieldTripCateringRequest = new();
    [ObservableProperty] bool showFood;

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    public static implicit operator NewFieldTrip(FieldTripViewModel vm) =>
        new(
              vm.PrimaryContactSearch.Selected.Id,
              vm.Transportation,
              vm.StudentsByClass,
              vm.ChaperoneSearch.AllSelected.Select(con => con.Id).ToImmutableArray(),
              vm.ShowFood ? (NewFieldTripCateringRequest)vm.FieldTripCateringRequest : null
        );

    public static FieldTripViewModel Get(FieldTripDetails model)
    {
        var vm = new FieldTripViewModel()
        {
            Id = model.eventId,
            StudentsByClass = StudentsByClassViewModel.Get(model.studentCount),
            Transportation = TransportationViewModel.Get(model.transportationDetails)
        };

        if (model.lunch.HasValue)
        {
            vm.FieldTripCateringRequest = FieldTripCateringRequestViewModel.Get(model.lunch.Value);
            vm.ShowFood = true;
        }

        vm.PrimaryContactSearch.Select(ContactViewModel.Get(model.primaryContact));

        foreach(var contact in model.chaperones)
        {
            vm.ChaperoneSearch.Select(ContactViewModel.Get(contact));
        }

        return vm;
    }
        
    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();

    [RelayCommand]
    public async Task Continue()
    {
        var result = await _service.PostFieldTripDetails(Id, this, OnError.DefaultBehavior(this));
        if (result.HasValue)
            ReadyToContinue?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task Delete()
    {
        var result = await _service.DeleteFieldTripDetails(Id, OnError.DefaultBehavior(this));
        if (result)
            Deleted?.Invoke(this, EventArgs.Empty);
    }
}

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

public partial class StudentsByClassViewModel :
    ObservableObject
{
    [ObservableProperty] int classI;
    [ObservableProperty] int classII;
    [ObservableProperty] int classIII;
    [ObservableProperty] int classIV;
    [ObservableProperty] int classV;
    [ObservableProperty] int classVI;
    [ObservableProperty] int classVII;
    [ObservableProperty] int classVIII;

    public static implicit operator StudentsByClassCount(StudentsByClassViewModel vm) =>
        new(vm.ClassI, vm.ClassII, vm.ClassIII, vm.ClassIV, vm.ClassV, vm.ClassVI, vm.ClassVII, vm.ClassVIII);

    public static StudentsByClassViewModel Get(StudentsByClassCount model) => new()
    {
        ClassI = model.classI,
        ClassII = model.classII,
        ClassIII = model.classIII,
        ClassIV = model.classIV,
        ClassV = model.classV,
        ClassVI = model.classVI,
        ClassVII = model.classVII,
        ClassVIII = model.classVIII
    };
}

public partial class FieldTripCateringRequestViewModel :
    ObservableObject
{
    public readonly CateringMenuService _service = ServiceHelper.GetService<CateringMenuService>();

    [ObservableProperty] CateringMenuCollectionViewModel menuCollection;
    [ObservableProperty] int numberOfLunches;
    [ObservableProperty] int diningInCount;
    [ObservableProperty] bool eatingAway;
    [ObservableProperty] TimeOnly pickupTime;
    
    public FieldTripCateringRequestViewModel()
    {
        MenuCollection = new(_service);
        MenuCollection.Menus = MenuCollection.Menus.Where(menu => menu.IsFieldTrip).ToImmutableArray();
        foreach (var menu in MenuCollection.Menus)
            menu.Items = menu.Items.Where(it => it.Item.FieldTripItem).ToImmutableArray();
    }

    public static implicit operator NewFieldTripCateringRequest(FieldTripCateringRequestViewModel vm) =>
        new(vm.NumberOfLunches,
            vm.MenuCollection.Menus.SelectMany(
                menu => menu.Items.Where(item => item.IsSelected))
            .Select(item => item.Item.Id)
            .ToImmutableArray(),
            vm.DiningInCount,
            vm.EatingAway,
            vm.PickupTime);

    public static FieldTripCateringRequestViewModel Get(FieldTripCateringRequest model)
    {
        var vm = new FieldTripCateringRequestViewModel()
        {
            NumberOfLunches = model.numberOfLunches,
            DiningInCount = model.diningInCount,
            EatingAway = model.eatingAway,
            PickupTime = model.pickupTime
        };

        vm.MenuCollection.LoadMenuSelections(model.menuItems);

        return vm;
    }
}