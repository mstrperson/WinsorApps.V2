using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class FacilitesEventViewModel :
    ObservableObject,
    IEventSubFormViewModel<FacilitesEventViewModel, FacilitiesEvent>,
    IErrorHandling,
    IModelCarrier<FacilitesEventViewModel, FacilitiesEvent>
{
    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] private string id = "";
    [ObservableProperty] private bool setup;
    [ObservableProperty] private bool presence;
    [ObservableProperty] private bool breakdown;
    [ObservableProperty] private bool overnight;
    [ObservableProperty] private bool parking;
    [ObservableProperty] private LocationSetupCollectionViewModel locations = new();
    [ObservableProperty] private bool hasLoaded;

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    public FacilitesEventViewModel()
    {
    }

    public Optional<FacilitiesEvent> Model { get; private set; } = Optional<FacilitiesEvent>.None();

    public static implicit operator NewFacilitiesEvent(FacilitesEventViewModel vm) =>
        new(vm.Setup, vm.Presence, vm.Breakdown, vm.Overnight, vm.Parking, vm.Locations);

    public void Clear()
    {
        Id = "";
        Setup = false;
        Presence = false;
        Breakdown = false;
        Overnight = false;
        Parking = false;
        Model = Optional<FacilitiesEvent>.None();
        Locations.Clear();
    }

    public void Load(FacilitiesEvent model)
    {
        if (model == default)
        {
            Clear();
            return;
        }

        Id = model.id;
        Setup = model.setup;
        Presence = model.presence;
        Breakdown = model.breakdown;
        Overnight = model.overnight;
        Parking = model.parking;
        Model = Optional<FacilitiesEvent>.Some(model);
        
        Locations.LoadSetupInformation(model.locations);
        HasLoaded = true;
    }

    private void ValidationFailMessage(string message)
    {
        OnError?.Invoke(this, new("Invalid Data Entered.", message));
    }

    [RelayCommand]
    public async Task Continue(bool template = false)
    {
        foreach(var setup in this.Locations.Setups)
        {
            if(string.IsNullOrEmpty(setup.Instructions))
            {
                ValidationFailMessage($"You must enter instructions for setup in {setup.LocationSearch.Selected.Label}");
                return;
            }
        }

        var result = await _service.PostFacilitiesEvent(Id, this, OnError.DefaultBehavior(this));
        if (result is not null)
        {
            Model = Optional<FacilitiesEvent>.Some(result);
            if(!template)
                ReadyToContinue?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    public async Task Delete()
    {
        var result = await _service.DeleteFacilitiesEvent(Id, OnError.DefaultBehavior(this));
        if (result)
            Deleted?.Invoke(this, EventArgs.Empty);
    }

    private FacilitesEventViewModel(FacilitiesEvent model) { Load(model); }

    public static FacilitesEventViewModel Get(FacilitiesEvent model) => new(model);
}

public partial class LocationSetupCollectionViewModel :
    ObservableObject
{
    [ObservableProperty] private ObservableCollection<LocationSetupViewModel> setups = [];
    [ObservableProperty] private LocationSetupViewModel selected = new();
    [ObservableProperty] private bool showSelected;

    public static implicit operator List<NewLocationSetup>(LocationSetupCollectionViewModel vm) =>
        [.. vm.Setups.Where(setup => setup.LocationSearch.IsSelected).Select(setup => (NewLocationSetup)setup)];

    public void LoadSetupInformation(IEnumerable<LocationSetupInstructions> setups)
    {
        Setups = [];
        foreach(var setup in setups)
        {
            var vm = LocationSetupViewModel.Get(setup);
            vm.Deleted += (_, _) => Setups.Remove(vm);
            Setups.Add(vm);
        }
    }

    [RelayCommand]
    public void Clear()
    {
        Setups = [];
        Selected = new();
        ShowSelected = false;
    }


    [RelayCommand]
    public void AddSetup()
    {
        var vm = new LocationSetupViewModel();
        vm.LocationSearch.OnSingleResult += (_, e) =>
        {
            vm.IsSelected = true;
        };

        vm.LocationSearch.OnSingleResult += (_, loc) =>
        {
            SaveSelected();
        };

        vm.Selected += (_, selected) => Selected = selected;
        vm.Deleted += (_, _) => Setups.Remove(vm);
        vm.SetupDate = DateTime.Today.AddDays(14);
        Selected = vm;
        ShowSelected = true;
    }

    [RelayCommand]
    public void SaveSelected()
    {
        if (!Selected.LocationSearch.IsSelected)
            return;

        var existing = Setups.FirstOrDefault(lsv => lsv.LocationSearch.Selected.Id == Selected.LocationSearch.Selected.Id);
        if(existing is null)
        {
            Setups.Add(Selected);
        }
        else
        {
            Setups.Remove(existing);
            Setups.Add(Selected);
        }

        ShowSelected = false;
    }

    [RelayCommand]
    public void DeleteSelected()
    {
        var existing = Setups.FirstOrDefault(lsv => lsv.LocationSearch.Selected.Id == Selected.LocationSearch.Selected.Id);
        if (existing is not null)
            Setups.Remove(existing);

        Selected = new();
        ShowSelected = false;
    }
}

public partial class LocationSetupViewModel :
    ObservableObject,
    ISelectable<LocationSetupViewModel>
{
    [ObservableProperty] private LocationSearchViewModel locationSearch = new() { CustomLocations = false, SelectionMode = SelectionMode.Single };
    [ObservableProperty] private string instructions = "";
    [ObservableProperty] private bool sandwichSign;
    [ObservableProperty] private DateTime setupDate;
    [ObservableProperty] private TimeSpan setupTime;
    [ObservableProperty] private bool isSelected;

    public LocationSetupInstructions Model { get; private set; }

    public event EventHandler<LocationSetupViewModel>? Selected;
    public event EventHandler? Deleted;

    public static implicit operator NewLocationSetup(LocationSetupViewModel vm) =>
        new(vm.LocationSearch.Selected.Id, vm.Instructions, vm.SandwichSign, vm.SetupDate.Add(vm.SetupTime));

    public static LocationSetupViewModel Get(LocationSetupInstructions model)
    {
        var vm = new LocationSetupViewModel()
        {
            Instructions = model.instructions,
            SandwichSign = model.includeSandwichSign,
            SetupDate = model.setupTime.Date,
            SetupTime = TimeOnly.FromDateTime(model.setupTime).ToTimeSpan(),
            Model = model
        };
        var lvm = LocationViewModel.Get(model.locationId);
        if (lvm is not null)
            vm.LocationSearch.Select(lvm);

        return vm;
    }

    [RelayCommand]
    public void Delete()
    {
        Deleted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}
