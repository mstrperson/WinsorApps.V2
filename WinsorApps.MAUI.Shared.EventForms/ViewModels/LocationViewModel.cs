using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using Location = WinsorApps.Services.EventForms.Models.Location;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class LocationViewModel :
    ObservableObject,
    IDefaultValueViewModel<LocationViewModel>,
    ICachedViewModel<LocationViewModel, Location, LocationService>,
    IErrorHandling
{
    [ObservableProperty] string id = "";
    [ObservableProperty] string label = "";
    [ObservableProperty] string type = "custom-location";
    
    /// <summary>
    /// Only applies to custom locations,
    /// Only Editable for New entries
    /// </summary>
    [ObservableProperty] bool isPublic;

    /// <summary>
    /// Flag for allowing the Create command and
    /// for editablility the IsPublic property
    /// </summary>
    [ObservableProperty] bool isNew = true;

    /// <summary>
    /// Flag for displaying the IsPublic property
    /// </summary>
    [ObservableProperty] bool isCustomLocation = true;

    private readonly LocationService _service = ServiceHelper.GetService<LocationService>();

    public event EventHandler<ErrorRecord>? OnError;

    public LocationViewModel()
    {

    }

    [RelayCommand]
    public async Task Create()
    {
        if (!string.IsNullOrEmpty(Id)) return; // Can't create a thing that already exists!

        var result = await _service.CreateCustomLocation(Label, IsPublic, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            this.Id = result.Value.id;
            this.Type = result.Value.type;
        }
    }

    [RelayCommand]
    public async Task Delete()
    {
        await _service.DeleteCustomLocation(Id, OnError.DefaultBehavior(this));
    }

    public static LocationViewModel Default => new();

    public static ConcurrentBag<LocationViewModel> ViewModelCache { get; private set; } = [];

    public static LocationViewModel Get(Location model)
    {
        var vm = ViewModelCache.FirstOrDefault(loc => loc.Id == model.id);
        if (vm is not null)
            return vm.Clone();

        vm = new()
        {
            Id = model.id,
            Label = model.label,
            Type = model.type,
            IsNew = false,
            IsCustomLocation = model.type == "custom-location"
        };
        ViewModelCache.Add(vm);
        return vm.Clone();
    }

    public static List<LocationViewModel> GetClonedViewModels(IEnumerable<Location> models)
    {
        List<LocationViewModel> result = [];
        foreach (var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(LocationService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

        _ = GetClonedViewModels(service.OnCampusLocations);
        _ = GetClonedViewModels(service.MyCustomLocations);
    }

    public LocationViewModel Clone() => (LocationViewModel)MemberwiseClone();
}

public partial class LocationSearchViewModel :
    ObservableObject,
    ICachedSearchViewModel<LocationViewModel>,
    IErrorHandling
{
    private readonly LocationService _service = ServiceHelper.GetService<LocationService>();

    public bool CustomLocations { get; set; }

    public LocationSearchViewModel()
    {
        _service.OnCacheRefreshed += 
            (_, _) => Available = LocationViewModel
                .GetClonedViewModels(
                    CustomLocations ? _service.MyCustomLocations : _service.OnCampusLocations)
                .ToImmutableArray();
        
        var initTask =_service.WaitForInit(OnError.DefaultBehavior(this));

        initTask.WhenCompleted(() =>
        {
            Available = LocationViewModel
                .GetClonedViewModels(
                    CustomLocations ? _service.MyCustomLocations : _service.OnCampusLocations)
                .ToImmutableArray();
        });
    }

    [ObservableProperty]
    private ImmutableArray<LocationViewModel> available = [];

    [ObservableProperty]
    private ImmutableArray<LocationViewModel> allSelected = [];

    [ObservableProperty]
    private ImmutableArray<LocationViewModel> options = [];

    [ObservableProperty]
    public LocationViewModel selected = LocationViewModel.Default;

    [ObservableProperty]
    private SelectionMode selectionMode = SelectionMode.Single;


    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private bool isSelected;
    [ObservableProperty]
    private bool showOptions;

    public event EventHandler<ImmutableArray<LocationViewModel>>? OnMultipleResult;
    public event EventHandler<LocationViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults;
    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public void Search()
    {
        var possible = Available
                .Where(loc => loc.Label.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase));
        if (!possible.Any())
            OnZeroResults?.Invoke(this, EventArgs.Empty);
        switch (SelectionMode)
        {
            case SelectionMode.Multiple:
                AllSelected = [.. possible];
                IsSelected = AllSelected.Length > 0;
                OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.Single:
                Options = [.. possible];
                if (Options.Length == 0)
                {
                    ShowOptions = false;
                    Selected = LocationViewModel.Default;
                    IsSelected = false;
                    return;
                }

                if (Options.Length == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    SearchText = Selected.Label;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = LocationViewModel.Default; 
                IsSelected = false;
                return;
            default: return;
        }
    }

    public void Select(LocationViewModel item)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                Selected = Available.FirstOrDefault(st => st.Id == item.Id) ?? LocationViewModel.Default;
                IsSelected = string.IsNullOrEmpty(Selected.Id);
                Options = [];
                ShowOptions = false;
                SearchText = Selected.Label;
                OnSingleResult?.Invoke(this, Selected);
                return;
            case SelectionMode.Multiple:
                var sta = Available.FirstOrDefault(st => st.Id == item.Id);
                if (sta is null) return;
                if (AllSelected.Contains(sta))
                    AllSelected = [.. AllSelected.Except([sta])];
                else
                    AllSelected = [.. AllSelected, sta];

                IsSelected = AllSelected.Length > 0;
                if (IsSelected)
                    OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    async Task IAsyncSearchViewModel<LocationViewModel>.Search() => await Task.Run(Search);
}