using Android.Provider;
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

public partial class FacilitesEventViewModel :
    ObservableObject,
    IEventSubFormViewModel,
    IErrorHandling

{
    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] bool setup;
    [ObservableProperty] bool presence;
    [ObservableProperty] bool breakdown;
    [ObservableProperty] bool overnight;
    [ObservableProperty] bool parking;
    [ObservableProperty] LocationSetupCollectionViewModel locations = new();

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    public static implicit operator NewFacilitiesEvent(FacilitesEventViewModel vm) =>
        new(vm.Setup, vm.Presence, vm.Breakdown, vm.Overnight, vm.Parking, vm.Locations);

    public static FacilitesEventViewModel Get(FacilitiesEvent model)
    {
        var vm = new FacilitesEventViewModel()
        {
            Setup = model.setup,
            Presence = model.presence,
            Breakdown = model.breakdown,
            Overnight = model.overnight,
            Parking = model.parking
        };
        vm.Locations.LoadSetupInformation(model.locations);
        return vm;
    }

    public Task Continue()
    {
        throw new NotImplementedException();
    }

    public Task Delete()
    {
        throw new NotImplementedException();
    }
}

public partial class LocationSetupCollectionViewModel :
    ObservableObject
{
    [ObservableProperty] ImmutableArray<LocationSetupViewModel> setups = [];
    [ObservableProperty] LocationSetupViewModel selected = new();
    [ObservableProperty] bool showSelected;

    public static implicit operator ImmutableArray<NewLocationSetup>(LocationSetupCollectionViewModel vm) =>
        vm.Setups.Where(setup => setup.LocationSearch.IsSelected).Select(setup => (NewLocationSetup)setup).ToImmutableArray();

    public void LoadSetupInformation(IEnumerable<LocationSetupInstructions> setups)
    {
        Setups = [];
        foreach(var setup in setups)
        {
            Setups = Setups.Add(LocationSetupViewModel.Get(setup));
        }
    }

    [RelayCommand]
    public void AddSetup()
    {
        Selected = new();
        Selected.Selected += (_, selected) => Selected = selected;
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
            Setups = Setups.Add(Selected);
        }
        else
        {
            Setups = Setups.Replace(existing, Selected);
        }

        ShowSelected = false;
    }

    [RelayCommand]
    public void DeleteSelected()
    {
        var existing = Setups.FirstOrDefault(lsv => lsv.LocationSearch.Selected.Id == Selected.LocationSearch.Selected.Id);
        if (existing is not null)
            Setups = Setups.Remove(existing);

        Selected = new();
        ShowSelected = false;
    }
}

public partial class LocationSetupViewModel :
    ObservableObject,
    ISelectable<LocationSetupViewModel>
{
    [ObservableProperty] LocationSearchViewModel locationSearch = new() { CustomLocations = false, SelectionMode = SelectionMode.Single };
    [ObservableProperty] string instructions = "";
    [ObservableProperty] bool sandwichSign;
    [ObservableProperty] DateOnly setupDate;
    [ObservableProperty] TimeOnly setupTime;
    [ObservableProperty] bool isSelected;

    public event EventHandler<LocationSetupViewModel>? Selected;

    public static implicit operator NewLocationSetup(LocationSetupViewModel vm) =>
        new(vm.LocationSearch.Selected.Id, vm.Instructions, vm.SandwichSign, vm.SetupDate.ToDateTime(vm.SetupTime));

    public static LocationSetupViewModel Get(LocationSetupInstructions model)
    {
        var vm = new LocationSetupViewModel()
        {
            Instructions = model.instructions,
            SandwichSign = model.includeSandwichSign,
            SetupDate = DateOnly.FromDateTime(model.setupTime),
            SetupTime = TimeOnly.FromDateTime(model.setupTime),
        };
        var lvm = LocationViewModel.Get(model.locationId);
        if (lvm is not null)
            vm.LocationSearch.Select(lvm);

        return vm;
    }
        

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}
