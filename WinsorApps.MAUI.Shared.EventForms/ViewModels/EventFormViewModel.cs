using Android.App.AppSearch;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
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
    IErrorHandling
{
    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string summary = "";
    [ObservableProperty] string description = "";
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
    [ObservableProperty] LocationSearchViewModel selectedLocations = new() { SelectionMode = SelectionMode.Multiple };
    [ObservableProperty] LocationSearchViewModel selectedCustomLocations = new() { SelectionMode = SelectionMode.Multiple, CustomLocations = true };
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

    [ObservableProperty] bool isSelected;
    public event EventHandler<EventFormViewModel>? Selected;
    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";

    [RelayCommand]
    public async Task LoadFacilities()
    {
        Busy = true;
        var facilities = await _service.GetFacilitiesEvent(Id, OnError.DefaultBehavior(this));
        if(!facilities.HasValue)
        {
            Facilites = new();
            HasFacilities = false;
            Busy = false;
            return;
        }
        
        Facilites = FacilitesEventViewModel.Get(facilities.Value);
        HasFacilities = true;

        Facilites.Deleted += (_, _) =>
        {
            Facilites = new();
            HasFacilities = false;
        };

        Busy = false;
    }

    [RelayCommand]
    public async Task LoadTech()
    {
        Busy = true;
        var tech = await _service.GetTechDetails(Id, OnError.DefaultBehavior(this));
        if (!tech.HasValue)
        {
            Tech = new();
            HasTech = false;
            Busy = false;
            return;
        }

        Tech = TechEventViewModel.Get(tech.Value);
        HasTech = true;

        Tech.Deleted += (_, _) =>
        {
            Tech = new();
            HasTech = false;
        };

        Busy = false;
    }

    [RelayCommand]
    public async Task LoadCatering()
    {
        Busy = true;
        var sub = await _service.GetCateringEvent(Id, OnError.DefaultBehavior(this));
        if (!sub.HasValue)
        {
            Catering = new();
            HasCatering = false;
            Busy = false;
            return;
        }

        Catering = CateringEventViewModel.Get(sub.Value);
        HasCatering = true;

        Catering.Deleted += (_, _) =>
        {
            Catering = new();
            HasCatering = false;
        };

        Busy = false;
    }

    [RelayCommand]
    public async Task LoadTheater()
    {
        Busy = true;
        var sub = await _service.GetTheaterDetails(Id, OnError.DefaultBehavior(this));
        if (!sub.HasValue)
        {
            Theater = new();
            HasTheater = false;
            Busy = false;
            return;
        }

        Theater = TheaterEventViewModel.Get(sub.Value);
        HasTheater = true;

        Theater.Deleted += (_, _) =>
        {
            Theater = new();
            HasTheater = false;
        };

        Busy = false;
    }

    [RelayCommand]
    public async Task LoadFieldTrip()
    {
        Busy = true;
        var sub = await _service.GetFieldTripDetails(Id, OnError.DefaultBehavior(this));
        if (!sub.HasValue)
        {
            FieldTrip = new();
            IsFieldTrip = false;
            Busy = false;
            return;
        }

        FieldTrip = FieldTripViewModel.Get(sub.Value);
        IsFieldTrip = true;

        FieldTrip.Deleted += (_, _) =>
        {
            FieldTrip = new();
            IsFieldTrip = false;
        };

        Busy = false;
    }

    [RelayCommand]
    public async Task LoadMarComm()
    {
        Busy = true;
        var sub = await _service.GetMarCommRequest(Id, OnError.DefaultBehavior(this));
        if (!sub.HasValue)
        {
            MarComm = new();
            HasMarComm = false;
            Busy = false;
            return;
        }

        MarComm = MarCommEventViewModel.Get(sub.Value);
        HasMarComm = true;

        MarComm.Deleted += (_, _) =>
        {
            MarComm = new();
            HasMarComm = false;
        };

        Busy = false;
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
            HasTheater = model.hasTheaterRequest
        };

        vm.StatusSelection.Select(eventForms.StatusLabels.First(status => status.label.Equals(model.status, StringComparison.InvariantCultureIgnoreCase));
        vm.LeaderSearch.Select(UserViewModel.Get(registrar.AllUsers.First(u => u.id == model.leaderId)));

        var locationService = ServiceHelper.GetService<LocationService>();

        foreach (var locId in model.selectedLocations ?? [])
        {
            vm.SelectedLocations.Select(LocationViewModel.Get(locationService.OnCampusLocations.First(loc => loc.id == locId)));
        }

        foreach (var locId in model.selectedCustomLocations ?? [])
        {
            vm.SelectedCustomLocations.Select(LocationViewModel.Get(locationService.MyCustomLocations.First(loc => loc.id == locId)));
        }

        if(model.attachments.HasValue)
        {
            vm.Attachments = model.attachments.Value.Select(header => new DocumentViewModel(header)).ToImmutableArray();
        }

        return vm.Clone();
    }

    public static List<EventFormViewModel> GetClonedViewModels(IEnumerable<EventFormBase> models) => models.Select(Get).ToList();

    public static Task Initialize(EventFormsService service, ErrorAction onError)
    {
        throw new NotImplementedException();
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
