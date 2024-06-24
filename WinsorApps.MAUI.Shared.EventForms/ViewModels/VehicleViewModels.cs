using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Intents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;


public partial class VehicleRequestCollectionViewModel :
    ObservableObject
{
    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();
    [ObservableProperty] VehicleCategoryCollectionViewModel categoryCollection;
    [ObservableProperty] ImmutableArray<VehicleRequestViewModel> requests = [];

    public static implicit operator ImmutableArray<NewVehicleRequest>(VehicleRequestCollectionViewModel vm) =>
        vm.Requests.Select(req => (NewVehicleRequest)req).ToImmutableArray();

    public VehicleRequestCollectionViewModel()
    {
        this.categoryCollection = new(_service);
        CategoryCollection.CreateRequested += AddRequest;
    }

    public void LoadRequests(IEnumerable<VehicleRequestViewModel> requests)
    {
        foreach (var request in requests)
        {
            AddRequest(null, request);
        }
    }

    public void AddRequest(object? _, VehicleRequestViewModel request)
    {
        var existingRequest = Requests.FirstOrDefault(req => req.Category.Id == request.Category.Id);
        if(existingRequest is not null)
        {
            existingRequest.CountRequested += request.CountRequested;
            return;
        }

        Requests.Add(request);
        request.Deleted += (_, _) => Requests = Requests.Remove(request);
    }
}

public partial class VehicleRequestViewModel :
    ObservableObject
{
    private static readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] VehicleCategoryViewModel category = new();
    [ObservableProperty] int countRequested;
    [ObservableProperty] string notes = "";

    public event EventHandler? Deleted;

    public static implicit operator NewVehicleRequest(VehicleRequestViewModel vm) => new(vm.Category.Id, vm.CountRequested, vm.Notes);

    public static VehicleRequestViewModel Get(VehicleRequest model) => new()
    {
        Category = VehicleCategoryViewModel.Get(_service.VehicleCategories.First(cat => cat.id == model.categoryId)),
        CountRequested = model.numberNeeded,
        Notes = model.notes
    };

    [RelayCommand]
    public void Delete() => Deleted?.Invoke(this, EventArgs.Empty);
}

public partial class VehicleCategoryCollectionViewModel :
    ObservableObject,
    IErrorHandling
{
    [ObservableProperty] ImmutableArray<VehicleCategoryViewModel> categories = [];

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<VehicleRequestViewModel>? CreateRequested;

    public VehicleCategoryViewModel this[string id] => Categories.First(cat => cat.Id == id);

    public VehicleCategoryCollectionViewModel(EventFormsService service)
    {
        var task = service.WaitForInit(OnError.DefaultBehavior(this));
        task.WhenCompleted(() =>
        {
            Categories = service.VehicleCategories.Select(VehicleCategoryViewModel.Get).ToImmutableArray();
            foreach (var cat in Categories)
                cat.CreateVehicleRequest += (sender, e) => CreateRequested?.Invoke(sender, e);
        });
    }
} 

public partial class VehicleCategoryViewModel :
    ObservableObject,
    ISelectable<VehicleCategoryViewModel>
{
    [ObservableProperty] string id = "";
    [ObservableProperty] string label = "";
    [ObservableProperty] int passengers;

    [ObservableProperty] bool isSelected;

    public event EventHandler<VehicleCategoryViewModel>? Selected;
    public event EventHandler<VehicleRequestViewModel>? CreateVehicleRequest;

    public static VehicleCategoryViewModel Get(VehicleCategory model) => new()
    {
        Id = model.id,
        Label = model.label,
        Passengers = model.passengers
    };

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }

    [RelayCommand]
    public void StartRequest()
    {
        CreateVehicleRequest?.Invoke(this, new() { Category = this });
    }
}
public partial class HiredBusViewModel :
    ObservableObject
{
    [ObservableProperty] int count;
    [ObservableProperty] TimeOnly departureTime;
    [ObservableProperty] TimeOnly pickupTime;
    [ObservableProperty] TimeOnly returnArrivalTime;
    [ObservableProperty] string instructions = "";
    [ObservableProperty] bool showHiredBusses;

    public static implicit operator FieldTripHiredBusRequest(HiredBusViewModel vm) =>
        new(vm.Count, vm.DepartureTime, vm.PickupTime, vm.ReturnArrivalTime, vm.Instructions);

    public static HiredBusViewModel Get(FieldTripHiredBusRequest model) => new()
    {
        Count = model.busCount,
        DepartureTime = model.departureTime,
        PickupTime = model.busPickupTime,
        ReturnArrivalTime = model.returnTime,
        Instructions = model.instructions,
        ShowHiredBusses = model.busCount > 0
    };

    [RelayCommand]
    public void Delete()
    {
        ShowHiredBusses = false;
        Count = 0;
    }
}
