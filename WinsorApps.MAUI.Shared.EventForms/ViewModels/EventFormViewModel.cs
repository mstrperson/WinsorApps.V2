using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;



public partial class EventFormViewModel :
    ObservableObject,
    ICachedViewModel<EventFormViewModel, EventFormBase, EventFormsService>,
    ISelectable<EventFormViewModel>,
    IBusyViewModel,
    IErrorHandling,
    IModelCarrier<EventFormViewModel, EventFormBase>
{
    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string summary = "";
    [ObservableProperty] string description = "";
    [ObservableProperty] EventTypeSelectionViewModel typeSelection = new();
    [ObservableProperty] EventTypeViewModel type = EventTypeViewModel.Get("default");
    [ObservableProperty] ApprovalStatusSelectionViewModel statusSelection = new();
    [ObservableProperty] DateTime startDate = DateTime.Today;
    [ObservableProperty] DateTime endDate = DateTime.Today;
    [ObservableProperty] TimeSpan startTime;
    [ObservableProperty] TimeSpan endTime;
    [ObservableProperty] UserViewModel creator = new();
    [ObservableProperty] UserSearchViewModel leaderSearch = new();
    [ObservableProperty] DateTime preapprovalDate = DateTime.Today;
    [ObservableProperty] int attendeeCount;
    [ObservableProperty] ObservableCollection<LocationViewModel> selectedLocations = [];
    [ObservableProperty] LocationSearchViewModel locationSearch = new() { SelectionMode = SelectionMode.Single };
    [ObservableProperty] ObservableCollection<LocationViewModel> selectedCustomLocations = [];
    [ObservableProperty] LocationSearchViewModel customLocationSearch = new() { SelectionMode = SelectionMode.Single, CustomLocations=true };

    [ObservableProperty] ImmutableArray<DocumentViewModel> attachments = [];
    [ObservableProperty] FacilitesEventViewModel facilites = new();
    [ObservableProperty] bool hasFacilities;
    [ObservableProperty] TechEventViewModel tech = new();
    [ObservableProperty] bool hasTech;
    [ObservableProperty] CateringEventViewModel catering = new();
    [ObservableProperty] bool hasCatering;
    [ObservableProperty] TheaterEventViewModel theater = new();
    [ObservableProperty] bool hasTheater;
    [ObservableProperty] FieldTripViewModel fieldTrip = new();
    [ObservableProperty] bool isFieldTrip;
    [ObservableProperty] MarCommEventViewModel marComm = new();
    [ObservableProperty] bool hasMarComm;

    [ObservableProperty] bool isNew = true;
    [ObservableProperty] bool isCreating;
    [ObservableProperty] bool isUpdating;
    [ObservableProperty] bool canEditBase = true;
    [ObservableProperty] bool canEditSubForms;

    [ObservableProperty] bool canEditCatering;

    [ObservableProperty] bool isSelected;
    public event EventHandler<EventFormViewModel>? Selected;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<EventFormViewModel>? TemplateRequested;
    public event EventHandler? SubFormContinuing;

    public event EventHandler<FacilitesEventViewModel>? FacilitesRequested;
    public event EventHandler<TechEventViewModel>? TechRequested;
    public event EventHandler<CateringEventViewModel>? CateringRequested;
    public event EventHandler<TheaterEventViewModel>? TheaterRequested;
    public event EventHandler<MarCommEventViewModel>? MarCommRequested;
    public event EventHandler<FieldTripViewModel>? FieldTripRequested;
    public event EventHandler? Deleted;
    public event EventHandler? Submitted;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";

    public EventFormBase Model { get; private set; }

    public EventFormViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        LeaderSearch.SetAvailableUsers(registrar.EmployeeList);
        CustomLocationSearch.SetCustomLocations(true);

        LocationSearch.OnSingleResult += (_, e) =>
        {
            var location = e.Clone();
            LocationSearch.ClearSelection();
            location.Selected += (_, _) => SelectedLocations.Remove(location);
            SelectedLocations.Add(location);
        };

        CustomLocationSearch.OnSingleResult += (_, e) =>
        {

            var location = e.Clone();
            CustomLocationSearch.ClearSelection();
            location.Selected += (_, _) => 
                SelectedCustomLocations.Remove(location);
            SelectedCustomLocations.Add(location);
        };

        Tech.Deleted += (_, _) =>
        {
            Tech.Clear();
            HasTech = false;
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };

        Tech.ReadyToContinue += (_, _) =>
        {
            HasTech = true;
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };



        Facilites.Deleted += (_, _) =>
        {
            Facilites.Clear();
            HasFacilities = false;
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };

        Facilites.ReadyToContinue += (_, _) =>
        {
            HasFacilities = true;
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };

        Facilites.OnError += (sender, err) => OnError?.Invoke(sender, err);


        Catering.Deleted += (_, _) =>
        {
            Catering.Clear(); 
            HasCatering = false;
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };

        Catering.ReadyToContinue += (_, _) =>
        {
            HasCatering = true;
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };

        Catering.OnError += (sender, err) => OnError?.Invoke(sender, err);

        Theater.Deleted += (_, _) =>
        {
            Theater.Clear();
            HasTheater = false;
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };

        Theater.ReadyToContinue += (_, _) =>
        {
            HasTheater = true;
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };

        Theater.OnError += (sender, err) => OnError?.Invoke(sender, err);

        FieldTrip.Deleted += (_, _) =>
        {
            FieldTrip.Clear();
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };

        FieldTrip.ReadyToContinue += (_, _) =>
        {
            Type = EventTypeViewModel.Get("Field Trip");
            SubFormContinuing?.Invoke(this, EventArgs.Empty);
        };

        FieldTrip.OnError += (sender, err) => OnError?.Invoke(sender, err);
    }

    [RelayCommand]
    public async Task Print()
    {
        //var download = await _service.
    }

    private static readonly ApiService _api = ServiceHelper.GetService<ApiService>();

    public static implicit operator NewEvent(EventFormViewModel vm) =>
        new(
            vm.Summary,
            vm.Description,
            vm.Type,
            vm.StartDate.Add(vm.StartTime),
            vm.EndDate.Add(vm.EndTime),
            _api.AuthUserId ?? "",
            vm.LeaderSearch.Selected.Id,
            DateOnly.FromDateTime(vm.PreapprovalDate),
            vm.AttendeeCount,
            null,
            vm.SelectedLocations.Select(loc => loc.Id).ToImmutableArray(),
            vm.SelectedCustomLocations.Select(loc => loc.Id).ToImmutableArray()
        );

    [RelayCommand]
    public async Task StartNewForm()
    {
        if (!string.IsNullOrEmpty(Id)) 
            return;
        Busy = true;

        var result = await _service.StartNewForm(this, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            Model = result.Value;
            CanEditBase = true;
            CanEditSubForms = true;
            IsCreating = true;
            IsNew = false;
            Id = result.Value.id;
        }

        Busy = false;
    }

    [RelayCommand]
    public void Template()
    {
        var clone = this.Clone();
        clone.IsSelected = false;
        clone.Id = "";
        clone.IsNew = true;
        clone.IsCreating = false;
        clone.IsUpdating = false;
        clone.CanEditBase = true;
        clone.CanEditSubForms = false;
        clone.Catering.Id = "";
        clone.Theater.Id = "";
        clone.Tech.Id = "";
        clone.Facilites.Id = "";
        clone.MarComm.Id = "";
        clone.FieldTrip.Id = "";
        clone.Model = new();
        
        TemplateRequested?.Invoke(this, clone);
    }

    [RelayCommand]
    public async Task StartUpdating()
    {
        if (string.IsNullOrEmpty(Id))
            return;
        Busy = true;
        var result = await _service.BeginUpdating(Id, this, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            Model = result.Value;
            StatusSelection.Select(result.Value.status);
            CanEditBase = true;
            CanEditSubForms = true;
            IsUpdating = true;
        }

        Busy = false;
    }

    [RelayCommand]
    public async Task CompleteSubmission()
    {
        if (!IsCreating)
            return;
        Busy = true;
        var result = await _service.CompleteSubmission(Id, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            Model = result.Value;
            CanEditBase = false;
            CanEditSubForms = false;
            CanEditCatering = false;
            IsNew = false;
            StatusSelection.Select(result.Value.status);
        }
        Busy = false;
        Submitted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task CompleteUpdate()
    {
        if (!IsUpdating)
            return;

        Busy = true;
        var result = await _service.CompleteUpdate(Id, OnError.DefaultBehavior(this));
        if (result.HasValue)
        {
            Model = result.Value;
            CanEditBase = false;
            CanEditSubForms = false;
            CanEditCatering = false;
            IsNew = false;
            StatusSelection.Select(result.Value.status);
        }
        Busy = false;
        Submitted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task Delete()
    {
        if (string.IsNullOrEmpty(Id))
            return;
        Busy = true;

        var result = await _service.DeleteEventForm(Id, OnError.DefaultBehavior(this));

        Busy = false;
        if(result)
            Deleted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void AddFacilities()
    {
        Facilites.Id = Id;
        FacilitesRequested?.Invoke(this, Facilites);
    }

    [RelayCommand]
    public void AddTech()
    {
        Tech.Id = Id;
        TechRequested?.Invoke(this, Tech);
    }


    [RelayCommand]
    public void AddCatering()
    {
        Catering.Id = Id;
        CateringRequested?.Invoke(this, Catering);
    }

    [RelayCommand]
    public void AddTheater()
    {
        Theater.Id = Id;
        TheaterRequested?.Invoke(this, Theater);
    }

    [RelayCommand]
    public void AddMarComm()
    {
        MarComm.Id = Id;
        MarCommRequested?.Invoke(this, MarComm);
    }

    [RelayCommand]
    public void AddFieldTrip()
    {
        Facilites.Id = Id;
        FacilitesRequested?.Invoke(this, Facilites);
    }

    [RelayCommand]
    public async Task LoadFacilities()
    {
        Busy = true;
        var facilities = await _service.GetFacilitiesEvent(Id, OnError.DefaultBehavior(this));
        if(!facilities.HasValue)
        {
            Facilites.Clear();
            HasFacilities = false;
            Busy = false;
            return;
        }
        
        Facilites.Load(facilities.Value);
        HasFacilities = true;
        Busy = false;
        FacilitesRequested?.Invoke(this, Facilites);
    }

    [RelayCommand]
    public async Task LoadTech()
    {
        Busy = true;
        var tech = await _service.GetTechDetails(Id, OnError.DefaultBehavior(this));
        if (!tech.HasValue)
        {
            Tech.Clear();
            HasTech = false;
            Busy = false;
            return;
        }

        Tech.Load(tech.Value);
        HasTech = true;

        Busy = false;

        TechRequested?.Invoke(this, Tech);
    }

    [RelayCommand]
    public async Task LoadCatering()
    {
        Busy = true;
        var sub = await _service.GetCateringEvent(Id, OnError.DefaultBehavior(this));
        if (!sub.HasValue)
        {
            Catering.Clear();
            HasCatering = false;
            Busy = false;
            return;
        }

        Catering.Load(sub.Value);
        HasCatering = true;


        Busy = false;

        CateringRequested?.Invoke(this, Catering);
    }

    [RelayCommand]
    public async Task LoadTheater()
    {
        Busy = true;
        var sub = await _service.GetTheaterDetails(Id, OnError.DefaultBehavior(this));
        if (!sub.HasValue)
        {
            Theater.Clear();
            HasTheater = false;
            Busy = false;
            return;
        }

        Theater.Load(sub.Value);
        HasTheater = true;

        Busy = false;
        TheaterRequested?.Invoke(this, Theater);
    }

    [RelayCommand]
    public async Task LoadFieldTrip()
    {
        Busy = true;
        var sub = await _service.GetFieldTripDetails(Id, OnError.DefaultBehavior(this));
        if (!sub.HasValue)
        {
            FieldTrip.Clear();
            IsFieldTrip = false;
            Busy = false;
            return;
        }

        FieldTrip.Load(sub.Value);
        IsFieldTrip = true;

        Busy = false;
        FieldTripRequested?.Invoke(this, FieldTrip);
    }

    [RelayCommand]
    public async Task LoadMarComm()
    {
        Busy = true;
        var sub = await _service.GetMarCommRequest(Id, OnError.DefaultBehavior(this));
        if (!sub.HasValue)
        {
            MarComm.Clear();
            HasMarComm = false;
            Busy = false;
            return;
        }

        MarComm.Load(sub.Value);
        HasMarComm = true;

        Busy = false;
        MarCommRequested?.Invoke(this, MarComm);
    }
    public static ConcurrentBag<EventFormViewModel> ViewModelCache { get; private set; } = [];

    public static EventFormViewModel Get(EventFormBase model)
    {
        var vm = ViewModelCache.FirstOrDefault(evt => evt.Id == model.id);
        if (vm is not null)
            return vm.Clone();

        var registrar = ServiceHelper.GetService<RegistrarService>();
        var eventForms = ServiceHelper.GetService<EventFormsService>();

        vm = new()
        {
            Id = model.id,
            Summary = model.summary,
            Description = model.description,
            Type = EventTypeViewModel.Get(model.type),
            StartDate = model.start.Date,
            StartTime = TimeOnly.FromDateTime(model.start).ToTimeSpan(),
            EndDate = model.end.Date,
            EndTime = TimeOnly.FromDateTime(model.end).ToTimeSpan(),
            Creator = UserViewModel.Get(registrar.AllUsers.First(u => u.id == model.creatorId)),
            AttendeeCount = model.attendeeCount,
            HasFacilities = model.hasFacilitiesInfo,
            HasTech = model.hasTechRequest,
            HasCatering = model.hasCatering,
            IsFieldTrip = model.hasFieldTripInfo,
            HasMarComm = model.hasMarCom,
            HasTheater = model.hasTheaterRequest,
            IsNew = false,
            IsCreating = model.status.Equals("creating", StringComparison.InvariantCultureIgnoreCase),
            IsUpdating = model.status.Equals("updating", StringComparison.InvariantCultureIgnoreCase),
            CanEditBase = true,
            CanEditSubForms = true,
            CanEditCatering = model.start > DateTime.Today.AddDays(14)
        };

        vm.StatusSelection.Select(eventForms.StatusLabels.First(status => status.label.Equals(model.status, StringComparison.InvariantCultureIgnoreCase)));
        vm.LeaderSearch.Select(UserViewModel.Get(registrar.AllUsers.First(u => u.id == model.leaderId)));

        var locationService = ServiceHelper.GetService<LocationService>();

        vm.SelectedLocations = [..LocationViewModel.GetClonedViewModels(locationService.OnCampusLocations.Where(loc => model.selectedLocations?.Contains(loc.id) ?? false))];
        foreach (var location in vm.SelectedLocations)
        {
            location.Selected += (_, _) =>
                vm.SelectedLocations.Remove(location);
        }

        vm.SelectedCustomLocations = [..LocationViewModel.GetClonedViewModels(locationService.MyCustomLocations.Where(loc => model.selectedCustomLocations?.Contains(loc.id) ?? false))];
        foreach (var location in vm.SelectedCustomLocations)
        {
            location.Selected += (_, _) => 
                vm.SelectedCustomLocations.Remove(location);
        }

        if (model.attachments.HasValue)
        {
            vm.Attachments = model.attachments.Value.Select(header => new DocumentViewModel(header)).ToImmutableArray();
        }

        return vm.Clone();
    }

    public static List<EventFormViewModel> GetClonedViewModels(IEnumerable<EventFormBase> models) => models.Select(Get).ToList();

    public static async Task Initialize(EventFormsService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

        _ = GetClonedViewModels(await service.GetMyCreatedEvents(default, default, onError));
        _ = GetClonedViewModels(await service.GetMyLeadEvents(default, default, onError));
    }

    public EventFormViewModel Clone() => (EventFormViewModel)MemberwiseClone();

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class EventTypeSelectionViewModel :
    ObservableObject,
    IErrorHandling
{
    [ObservableProperty] ImmutableArray<EventTypeViewModel> types;
    [ObservableProperty] EventTypeViewModel selected = EventTypeViewModel.Get("default");
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool showList;

    public event EventHandler<ErrorRecord>? OnError;
    public EventTypeSelectionViewModel()
    {
        var service = ServiceHelper.GetService<EventFormsService>();
        var task = service.WaitForInit(OnError.DefaultBehavior(this));
        task.WhenCompleted(() =>
        {
            Types = EventTypeViewModel.GetClonedViewModels(service.EventTypes).ToImmutableArray();
            foreach (var vm in Types)
                vm.Selected += (_, _) => Select(vm);
        });
    }

    [RelayCommand]
    public void ToggleShowList()
    {
        ShowList = !ShowList;
        IsSelected = !ShowList;
    }

    [RelayCommand]
    public void Select(string type)
    {
        var vm = Types.FirstOrDefault(t => t.Type.Equals(type, StringComparison.InvariantCultureIgnoreCase));
        if (vm is not null)
        {
            Selected = vm;
            IsSelected = true;
            ShowList = false;
        }
    }
}

public partial class EventTypeViewModel :
    ObservableObject,
    ICachedViewModel<EventTypeViewModel, string, EventFormsService>,
    ISelectable<EventTypeViewModel>
{
    public string Type { get; init; } = "";

    [ObservableProperty] bool isSelected;
    
    public static implicit operator string(EventTypeViewModel vm) => vm.Type;

    private EventTypeViewModel() { }

    public static ConcurrentBag<EventTypeViewModel> ViewModelCache { get; private set; } = [];

    public event EventHandler<EventTypeViewModel>? Selected;

    public static EventTypeViewModel Get(string model)
    {
        var vm = ViewModelCache.FirstOrDefault(type => model.Equals((string)type, StringComparison.InvariantCultureIgnoreCase));
        if (vm is not null)
            return vm.Clone();

        vm = new() { Type = model };
        ViewModelCache.Add(vm);

        return vm.Clone();
    }

    public static List<EventTypeViewModel> GetClonedViewModels(IEnumerable<string> models) => models.Select(Get).ToList();

    public static async Task Initialize(EventFormsService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);
        _ = GetClonedViewModels(service.EventTypes);
    }

    public EventTypeViewModel Clone() => (EventTypeViewModel)this.MemberwiseClone();

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class ApprovalStatusSelectionViewModel :
    ObservableObject,
    IErrorHandling
{
    [ObservableProperty] ImmutableArray<ApprovalStatusViewModel> statusList = [];
    [ObservableProperty] ApprovalStatusViewModel selected = new();
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool showList;

    public ApprovalStatusSelectionViewModel()
    {
        var service = ServiceHelper.GetService<EventFormsService>();
        var task = service.WaitForInit(OnError.DefaultBehavior(this));
        task.WhenCompleted(() =>
        {
            StatusList = ApprovalStatusViewModel.GetClonedViewModels(service.StatusLabels).ToImmutableArray();
            foreach (var item in statusList)
            {
                item.Selected += (_, _) =>
                {
                    Selected = item;
                    IsSelected = true;
                    ShowList = false;
                };
            }
        });
    }

    [RelayCommand]
    public void OpenList() => ShowList = true;

    public void Select(string status)
    {
        var vm = StatusList.FirstOrDefault(st => st.Label.Equals(status, StringComparison.InvariantCultureIgnoreCase));
        if (vm is null)
        {
            var service = ServiceHelper.GetService<EventFormsService>();
            var model = service.StatusLabels.FirstOrDefault(st => st.label.Equals(status, StringComparison.InvariantCultureIgnoreCase));
            if(model != default)
                Select(model);
            return;
        }

        Selected = vm;
        IsSelected = true;
        ShowList = false;
    }

    public void Select(ApprovalStatus status)
    {
        var vm = StatusList.FirstOrDefault(st => st.Id == status.id);
        if (vm is null)
        {
            vm = ApprovalStatusViewModel.Get(status);
            vm.Selected += (_, _) =>
            {
                Selected = vm;
                IsSelected = true;
                ShowList = false;
            };
            StatusList = StatusList.Add(vm);
        }

        Selected = vm;
        IsSelected = true;
        ShowList = false;
    }

    public event EventHandler<ErrorRecord>? OnError;
}

public partial class ApprovalStatusViewModel :
    ObservableObject,
    ICachedViewModel<ApprovalStatusViewModel, ApprovalStatus, EventFormsService>,
    ISelectable<ApprovalStatusViewModel>
{
    [ObservableProperty] string id = "";
    [ObservableProperty] string label = "";

    [ObservableProperty] bool isSelected;

    public static ConcurrentBag<ApprovalStatusViewModel> ViewModelCache { get; private set; } = [];

    public event EventHandler<ApprovalStatusViewModel>? Selected;

    public static ApprovalStatusViewModel Get(ApprovalStatus model)
    {
        var vm = ViewModelCache.FirstOrDefault(status => status.Id == model.id);
        if (vm is not null)
            return vm.Clone();

        vm = new()
        {
            Id = model.id,
            Label = model.label
        };

        ViewModelCache.Add(vm);
        return vm.Clone();
    }

    public static List<ApprovalStatusViewModel> GetClonedViewModels(IEnumerable<ApprovalStatus> models) => models.Select(Get).ToList();

    public static async Task Initialize(EventFormsService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

        _ = GetClonedViewModels(service.StatusLabels);
    }

    public ApprovalStatusViewModel Clone() => (ApprovalStatusViewModel)MemberwiseClone();

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}
