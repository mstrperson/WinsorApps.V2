using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
    [ObservableProperty] ObservableCollection<VehicleRequestViewModel> requests = [];

    public static implicit operator ImmutableArray<NewVehicleRequest>(VehicleRequestCollectionViewModel vm) =>
        vm.Requests.Select(req => (NewVehicleRequest)req).ToImmutableArray();

    public event EventHandler? Cleared;

    public VehicleRequestCollectionViewModel()
    {
        this.categoryCollection = new(_service);
        CategoryCollection.CreateRequested += AddRequest;
    }

    [RelayCommand]
    public void ClearRequests()
    {
        Requests = [];
        foreach (var cat in CategoryCollection.Categories)
            cat.IsSelected = false;
        Cleared?.Invoke(this, EventArgs.Empty);
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
        var category = CategoryCollection.Categories.First(cat => cat.Id == request.Category.Id);
        if(existingRequest is not null)
        {
            existingRequest.CountRequested += request.CountRequested;
            return;
        }

        Requests.Add(request);
        request.Deleted += (_, _) =>
        {
            category.IsSelected = false;
            Requests.Remove(request);
        };
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
        Categories = service.VehicleCategories.Select(VehicleCategoryViewModel.Get).ToImmutableArray();
        foreach (var cat in Categories)
            cat.CreateVehicleRequest += (sender, e) => CreateRequested?.Invoke(sender, e);
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
    [ObservableProperty] TimeSpan departureTime;
    [ObservableProperty] TimeSpan pickupTime;
    [ObservableProperty] TimeSpan returnArrivalTime;
    [ObservableProperty] string instructions = "";
    [ObservableProperty] bool showHiredBusses;

    public static implicit operator FieldTripHiredBusRequest(HiredBusViewModel vm) =>
        new(vm.Count, TimeOnly.FromTimeSpan(vm.DepartureTime), TimeOnly.FromTimeSpan(vm.PickupTime), TimeOnly.FromTimeSpan(vm.ReturnArrivalTime), vm.Instructions);

    public static HiredBusViewModel Get(FieldTripHiredBusRequest model) => new()
    {
        Count = model.busCount,
        DepartureTime = model.departureTime.ToTimeSpan(),
        PickupTime = model.busPickupTime.ToTimeSpan(),
        ReturnArrivalTime = model.returnTime.ToTimeSpan(),
        Instructions = model.instructions,
        ShowHiredBusses = model.busCount > 0
    };

    [RelayCommand]
    public void Delete()
    {
        ShowHiredBusses = false;
        Count = 0;
    }

    [RelayCommand]
    public void Show() => ShowHiredBusses = true;
}
