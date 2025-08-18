using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using Location = WinsorApps.Services.EventForms.Models.Location;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class LocationViewModel :
    ObservableObject,
    IDefaultValueViewModel<LocationViewModel>,
    ICachedViewModel<LocationViewModel, Location, LocationService>,
    ISelectable<LocationViewModel>,
    IErrorHandling,
    IModelCarrier<LocationViewModel, Location>
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string label = "";
    [ObservableProperty] private string type = "custom-location";
    
    /// <summary>
    /// Only applies to custom locations,
    /// Only Editable for New entries
    /// </summary>
    [ObservableProperty] private bool isPublic;

    /// <summary>
    /// Flag for allowing the Create command and
    /// for editablility the IsPublic property
    /// </summary>
    [ObservableProperty] private bool isNew = true;

    /// <summary>
    /// Flag for displaying the IsPublic property
    /// </summary>
    [ObservableProperty] private bool isCustomLocation = true;

    [ObservableProperty] private bool isSelected;

    private readonly LocationService _service = ServiceHelper.GetService<LocationService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<LocationViewModel>? Created;
    public event EventHandler<LocationViewModel>? Selected;

    public LocationViewModel()
    {

    }

    [RelayCommand]
    public async Task Create()
    {
        if (!string.IsNullOrEmpty(Id)) return; // Can't create a thing that already exists!

        var result = await _service.CreateCustomLocation(Label, IsPublic, OnError.DefaultBehavior(this));
        if(result is not null)
        {
            this.Id = result.id;
            this.Type = result.type;
        }

        Created?.Invoke(this, this);
    }

    [RelayCommand]
    public async Task Delete()
    {
        await _service.DeleteCustomLocation(Id, OnError.DefaultBehavior(this));
    }

    public static LocationViewModel Empty => new();

    public static List<LocationViewModel> ViewModelCache { get; private set; } = [];

    public static LocationViewModel? Get(string locationId, bool custom = false)
    {
        var service = ServiceHelper.GetService<LocationService>();
        var location = (custom ? service.MyCustomLocations : service.OnCampusLocations).FirstOrDefault(loc => loc.id == locationId) ?? Location.None;
        if (location.id == locationId)
            return Get(location);
        return null;
    }

    public Optional<Location> Model { get; private set; } = Optional<Location>.None();

    public static LocationViewModel Get(Location model)
    {
        var vm = ViewModelCache.FirstOrDefault(loc => loc.Id == model.id && loc.Type == model.type);
        if (vm is not null)
            return vm.Clone();

        vm = new()
        {
            Model = Optional<Location>.Some(model),
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

    public LocationViewModel Clone() => new()
    {
        Model = Model with { },
        Id = Id,
        IsNew = IsNew,
        IsCustomLocation = IsCustomLocation,
        Label = Label,
        IsPublic = IsPublic,
        IsSelected = false,
        Type = Type
    };

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class LocationSearchViewModel :
    ObservableObject,
    ICachedSearchViewModel<LocationViewModel>,
    IErrorHandling
{
    private readonly LocationService _service = ServiceHelper.GetService<LocationService>();

    [ObservableProperty] private bool customLocations;

    public LocationSearchViewModel()
    {
        _service.OnCacheRefreshed +=
            (_, _) =>
            {
                Available = [..LocationViewModel
                    .GetClonedViewModels(
                        CustomLocations ? _service.MyCustomLocations : _service.OnCampusLocations)];
                foreach (var loc in Available)
                {
                    loc.Selected += (_, _) => 
                        Select(loc);
                }
            };

        Available = [..LocationViewModel
            .GetClonedViewModels(
                CustomLocations ? _service.MyCustomLocations : _service.OnCampusLocations)];

        foreach (var loc in Available)
        {
            loc.Selected += (_, _) => 
                Select(loc);
        }
    }

    public void SetCustomLocations(bool custom)
    {
        CustomLocations = custom;

        Available = [..LocationViewModel
            .GetClonedViewModels(
                CustomLocations ? _service.MyCustomLocations : _service.OnCampusLocations)]; 
        
        foreach (var loc in Available)
        {
            loc.Selected += (_, _) => 
                Select(loc);
        }
    }

    [ObservableProperty]
    private ObservableCollection<LocationViewModel> available = [];

    [ObservableProperty]
    private ObservableCollection<LocationViewModel> allSelected = [];

    [ObservableProperty]
    private ObservableCollection<LocationViewModel> options = [];

    [ObservableProperty]
    public LocationViewModel selected = LocationViewModel.Empty;

    [ObservableProperty]
    private SelectionMode selectionMode = SelectionMode.Single;


    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private bool isSelected;
    [ObservableProperty]
    private bool showOptions;

    [ObservableProperty] private bool showCreate;

    [ObservableProperty] private LocationViewModel newItem = new();

    public event EventHandler<ObservableCollection<LocationViewModel>>? OnMultipleResult;
    public event EventHandler<LocationViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults;
    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public void ClearSelection()
    {
        Selected = new();
        IsSelected = false;
        SearchText = "";
        AllSelected = [];
        Options = [];
        ShowOptions = false;
        ShowCreate = false;
    }

    [RelayCommand]
    public void Create()
    {
        if (!CustomLocations) return;

        ShowCreate = true;
        NewItem = new() { Label = SearchText, IsCustomLocation = true, IsNew = true };
        NewItem.Selected += (_, e) => Select(e);
        NewItem.Created += (_, e) =>
        {
            ShowCreate = false;
            Available.Add(e);
            Select(e);
        };

    }

    [RelayCommand]
    public void CancelCreate() => ShowCreate = false;

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
                IsSelected = AllSelected.Count > 0;
                OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.Single:
                Options = [.. possible];
                if (Options.Count == 0)
                {
                    ShowOptions = false;
                    Selected = LocationViewModel.Empty;
                    IsSelected = false;
                    return;
                }

                if (Options.Count == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    SearchText = Selected.Label;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = LocationViewModel.Empty; 
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
                Selected = Available.FirstOrDefault(st => st.Id == item.Id && st.Type == item.Type) ?? LocationViewModel.Empty;
                IsSelected = string.IsNullOrEmpty(Selected.Id);
                Options = [];
                ShowOptions = false;
                SearchText = Selected.Label;
                OnSingleResult?.Invoke(this, Selected);
                return;
            case SelectionMode.Multiple:
                var sta = Available.FirstOrDefault(st => st.Id == item.Id && st.Type == item.Type);
                if (sta is null) 
                    return;
                
                AllSelected.Add(sta);
                AllSelected = [.. AllSelected.DistinctBy(st => st.Id)];

                IsSelected = AllSelected.Count > 0;
                if (IsSelected)
                    OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    async Task IAsyncSearchViewModel<LocationViewModel>.Search() => await Task.Run(Search);
}