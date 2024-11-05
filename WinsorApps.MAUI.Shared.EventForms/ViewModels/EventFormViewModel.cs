﻿using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.EventForms.Services.Admin;
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
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string summary = "";
    [ObservableProperty] string description = "";
    [ObservableProperty] EventTypeSelectionViewModel typeSelection = new() { Selected = EventTypeViewModel.Get("default") };
    [ObservableProperty] EventTypeViewModel type = EventTypeViewModel.Get("default");
    [ObservableProperty] ApprovalStatusSelectionViewModel statusSelection = new();
    [ObservableProperty] DateTime startDate = DateTime.Today;
    [ObservableProperty] DateTime endDate = DateTime.Today;
    [ObservableProperty] TimeSpan startTime;
    [ObservableProperty] TimeSpan endTime;
    [ObservableProperty] UserViewModel creator = new();
    [ObservableProperty] private UserViewModel leader = new();
    [ObservableProperty] UserSearchViewModel leaderSearch = new();
    [ObservableProperty] DateTime preapprovalDate = DateTime.Today;
    [ObservableProperty] int attendeeCount;
    [ObservableProperty] ObservableCollection<LocationViewModel> selectedLocations = [];
    [ObservableProperty] LocationSearchViewModel locationSearch = new() { SelectionMode = SelectionMode.Single };
    [ObservableProperty] ObservableCollection<LocationViewModel> selectedCustomLocations = [];
    [ObservableProperty] LocationSearchViewModel customLocationSearch = new() { SelectionMode = SelectionMode.Single, CustomLocations=true };

    [ObservableProperty] AttachmentCollectionViewModel attachments = new();
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
    [ObservableProperty] bool isCreating = false;
    [ObservableProperty] bool isUpdating = false;
    [ObservableProperty] bool canEditBase = true;
    [ObservableProperty] bool canEditSubForms = false;

    [ObservableProperty] bool canEditCatering = false;

    [ObservableProperty] bool hasLoadedOnce = false;

    [ObservableProperty] bool isSelected;
    public event EventHandler<EventFormViewModel>? Selected;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<EventFormViewModel>? TemplateRequested;

    public event EventHandler<FacilitesEventViewModel>? FacilitesRequested;
    public event EventHandler<TechEventViewModel>? TechRequested;
    public event EventHandler<CateringEventViewModel>? CateringRequested;
    public event EventHandler<TheaterEventViewModel>? TheaterRequested;
    public event EventHandler<MarCommEventViewModel>? MarCommRequested;
    public event EventHandler<FieldTripViewModel>? FieldTripRequested;
    public event EventHandler? Deleted;
    public event EventHandler? Submitted;

    public event EventHandler? ApproveRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? ApproveRoomRequested;

    [ObservableProperty] bool isPending;
    [ObservableProperty] bool isDeleted;
    [ObservableProperty] bool needsRoomApproval;

    [ObservableProperty]
    bool userIsAdmin = IsAdmin;
    [ObservableProperty]
    bool userIsRegistrar = IsRegistrar;
    private static bool IsAdmin
    {
        get
        {
            try
            {
                var admin = ServiceHelper.GetService<EventsAdminService>();
                if (admin is null)
                    return false;
            }
            catch
            {
                return false;
            }
            return _registrar.MyRoles.Intersect(["Winsor - Events Admin", "System Admin"]).Any();
        }
    }
    private static bool IsRegistrar
    {
        get
        {
            try
            {
                var admin = ServiceHelper.GetService<EventsAdminService>();
                if (admin is null)
                    return false;
            }
            catch
            {
                return false;
            }
            return _registrar.MyRoles.Intersect(["Registrar", "System Admin"]).Any();
        }
    }

    private static readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";

    public OptionalStruct<EventFormBase> Model { get; private set; } = OptionalStruct<EventFormBase>.None();

    public EventFormViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        LeaderSearch.SetAvailableUsers(registrar.EmployeeList);
        LeaderSearch.OnSingleResult += (_, leader) =>
        {
            Leader = leader;
            Leader.IsSelected = true;
        };
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
        };

        Tech.ReadyToContinue += (_, _) =>
        {
            HasTech = true;
        };

        Facilites.Deleted += (_, _) =>
        {
            Facilites.Clear();
            HasFacilities = false;
        };

        Facilites.ReadyToContinue += (_, _) =>
        {
            HasFacilities = true;
        };

        Facilites.OnError += (sender, err) => OnError?.Invoke(sender, err);


        Catering.Deleted += (_, _) =>
        {
            Catering.Clear(); 
            HasCatering = false;
        };

        Catering.ReadyToContinue += (_, _) =>
        {
            HasCatering = true;
        };

        Catering.OnError += (sender, err) => OnError?.Invoke(sender, err);

        Theater.Deleted += (_, _) =>
        {
            Theater.Clear();
            HasTheater = false;
        };

        Theater.ReadyToContinue += (_, _) =>
        {
            HasTheater = true;
        };

        Theater.OnError += (sender, err) => OnError?.Invoke(sender, err);

        FieldTrip.Deleted += (_, _) =>
        {
            FieldTrip.Clear();
        };

        FieldTrip.ReadyToContinue += (_, _) =>
        {
            Type = EventTypeViewModel.Get("Field Trip");
        };

        FieldTrip.OnError += (sender, err) => OnError?.Invoke(sender, err);
    }

    [RelayCommand]
    public async Task Print()
    {
        var download = await _service.DownloadPdf(Id, OnError.DefaultBehavior(this));
        if(download.Length > 0)
        {
            using MemoryStream ms = new MemoryStream(download);
            var result = await FileSaver.SaveAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{Summary}.pdf", ms);
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug,
                $"Downloaded PDF for {Summary}.",
                $"FileSaverResult has status IsSuccessful: {result.IsSuccessful}",
                $"Saved file to: {result.FilePath ?? "nowhere"}");
        }
    }

    private static readonly ApiService _api = ServiceHelper.GetService<ApiService>();

    public static implicit operator NewEvent(EventFormViewModel vm) =>
        new(
            vm.Summary,
            vm.Description,
            vm.Type ?? EventTypeViewModel.Get("default"),
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
    public void Approve()
    {
        ApproveRequested?.Invoke(this, EventArgs.Empty);
        IsPending = false;
        IsDeleted = false;
    }

    [RelayCommand]
    public void Reject()
    {
        DeleteRequested?.Invoke(this, EventArgs.Empty);
        IsDeleted = true;
        IsPending = false;
    }

    [RelayCommand]
    public void ApproveRoom()
    {
        ApproveRoomRequested?.Invoke(this, EventArgs.Empty);
        NeedsRoomApproval = false;
    }

    [RelayCommand]
    public void ClearLeader()
    {
        Leader = new() { IsSelected = false };
        LeaderSearch.ClearSelection();
    }
    
    protected void ValidationFailed(string message)
    {
        OnError?.Invoke(this, new("Invalid Form Data", message));
    }

    [RelayCommand]
    public async Task StartNewForm()
    {
        if (!string.IsNullOrEmpty(Id)) 
            return;

        if(string.IsNullOrWhiteSpace(Summary))
        {
            ValidationFailed("Event Name Cannot Be Empty.");
            return;
        }

        if(string.IsNullOrWhiteSpace(Description))
        {
            ValidationFailed("Event Description Cannot Be Empty.");
            return;
        }

        if(!LeaderSearch.IsSelected)
        {
            ValidationFailed("You must indicate the evnet Leader.");
            return;
        }

        

        Busy = true;

        var result = await _service.StartNewForm(this, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            Model = OptionalStruct<EventFormBase>.Some(result.Value);
            CanEditBase = true;
            CanEditSubForms = true;
            IsCreating = true;
            IsNew = false;
            Id = result.Value.id;
            IsFieldTrip = result.Value.type.StartsWith("Field", StringComparison.InvariantCultureIgnoreCase);
            StatusSelection.Select(result.Value.status);
            Attachments = new(result.Value);
            Type = EventTypeViewModel.Get(result.Value.type);
            
            if (Type == "Field Trip")
            {
                IsFieldTrip = true;
                await LoadFieldTrip();
            }
        }

        Busy = false;
    }

    [RelayCommand]
    public async Task Template()
    {
        var clone = new EventFormViewModel
        {
            Summary = Summary,
            Description = Description,
            LeaderSearch =
            {
                Selected = Leader.Clone(),
                IsSelected = true
            }
        };
        clone.PreapprovalDate = DateTime.Today;
        clone.AttendeeCount = AttendeeCount;
        clone.Creator = Creator.Clone();
        clone.StartTime = StartTime;
        clone.EndTime = EndTime;

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
        clone.StatusSelection.Select("Draft");
        clone.HasLoadedOnce = false;
        clone.StartDate = StartDate < DateTime.Today.AddMonths(-1) ? StartDate.AddYears(1) : StartDate.AddDays(7);
        clone.EndDate = clone.StartDate.Add(EndDate - StartDate);
        clone.Leader = LeaderSearch.Selected;
        
        foreach (var customLocation in SelectedCustomLocations)
            clone.SelectedCustomLocations.Add(customLocation.Clone());
        foreach (var location in SelectedLocations)
            clone.SelectedLocations.Add(location.Clone());

        await clone.StartNewForm();
        if (string.IsNullOrEmpty(clone.Id))
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Failed to start template of {Summary}");
            return;
        }
        clone.HasCatering = Model.Reduce(EventFormBase.Empty).hasCatering;
        if (Model.Reduce(EventFormBase.Empty).hasCatering)
        {
            var stuff = await _service.GetCateringEvent(Id, OnError.DefaultBehavior(this));
            if (stuff.HasValue)
            {
                clone.Catering.Load(stuff.Value);
                clone.Catering.Id = clone.Id;
                await clone.Catering.Continue(true);
            }
        }

        clone.HasFacilities = Model.Reduce(EventFormBase.Empty).hasFacilitiesInfo;
        if (Model.Reduce(EventFormBase.Empty).hasFacilitiesInfo)
        {
            var stuff = await _service.GetFacilitiesEvent(Id, OnError.DefaultBehavior(this));
            if (stuff.HasValue)
            {
                clone.Facilites.Load(stuff.Value);
                clone.Facilites.Id = clone.Id;
                await clone.Facilites.Continue(true);
            }
        }
        clone.HasTech = Model.Reduce(EventFormBase.Empty).hasTechRequest;
        if (Model.Reduce(EventFormBase.Empty).hasTechRequest)
        {
            var stuff = await _service.GetTechDetails(Id, OnError.DefaultBehavior(this));
            if (stuff.HasValue)
            {
                clone.Tech.Load(stuff.Value);
                clone.Tech.Id = clone.Id;
                await clone.Tech.Continue(true);
            }
        }
        clone.HasTheater = Model.Reduce(EventFormBase.Empty).hasTheaterRequest;
        if (Model.Reduce(EventFormBase.Empty).hasTheaterRequest)
        {
            var stuff = await _service.GetTheaterDetails(Id, OnError.DefaultBehavior(this));
            if (stuff.HasValue)
            {
                clone.Theater.Load(stuff.Value);
                clone.Theater.Id = clone.Id;
                await clone.Theater.Continue(true);
            }
        }
        clone.HasMarComm = Model.Reduce(EventFormBase.Empty).hasMarCom;
        if (Model.Reduce(EventFormBase.Empty).hasMarCom)
        {
            var stuff = await _service.GetMarCommRequest(Id, OnError.DefaultBehavior(this));
            if (stuff.HasValue)
            {
                clone.MarComm.Load(stuff.Value);
                clone.MarComm.Id = clone.Id;
                await clone.MarComm.Continue(true);
            }
        }
        clone.IsFieldTrip = Model.Reduce(EventFormBase.Empty).hasFieldTripInfo;
        if (Model.Reduce(EventFormBase.Empty).hasFieldTripInfo)
        {
            var stuff = await _service.GetFieldTripDetails(Id, OnError.DefaultBehavior(this));
            if (stuff.HasValue)
            {
                clone.FieldTrip.Load(stuff.Value);
                clone.FieldTrip.Id = clone.Id;
                await clone.FieldTrip.Continue(true);
            }
        }

        

        var eventList = ServiceHelper.GetService<EventListViewModel>();
        eventList.AddEvents([clone]);
        
        TemplateRequested?.Invoke(this, clone);
    }

    [RelayCommand]
    public async Task StartUpdating()
    {
        if (string.IsNullOrEmpty(Id))
            return;
        Busy = true;
        var result = await _service.UpdateEvent(Id, this, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            Model = OptionalStruct<EventFormBase>.Some(result.Value);
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

        var updatedBase = await _service.UpdateEvent(Id, this, OnError.DefaultBehavior(this));

        if(!updatedBase.HasValue)
        {
            Busy = false;
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Failed to update event {Summary} when attempting to complete submission");
            return;
        }

        Model = OptionalStruct<EventFormBase>.Some(updatedBase.Value);

        var result = await _service.CompleteSubmission(Id, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            Model = OptionalStruct<EventFormBase>.Some(result.Value);
            CanEditBase = false;
            CanEditSubForms = false;
            CanEditCatering = false;
            IsNew = false;
            IsFieldTrip = result.Value.type.Contains("Field", StringComparison.InvariantCultureIgnoreCase);
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

        var updatedBase = await _service.UpdateEvent(Id, this, OnError.DefaultBehavior(this));

        if (!updatedBase.HasValue)
        {
            Busy = false;
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Failed to update event {Summary} when attempting to complete submission");
            return;
        }

        Model = OptionalStruct<EventFormBase>.Some(updatedBase.Value);

        var result = await _service.CompleteUpdate(Id, OnError.DefaultBehavior(this));
        if (result.HasValue)
        {
            Model = OptionalStruct<EventFormBase>.Some(result.Value);
            CanEditBase = false;
            CanEditSubForms = false;
            CanEditCatering = false;
            IsNew = false;
            IsFieldTrip = result.Value.type.Contains("Field", StringComparison.InvariantCultureIgnoreCase);
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
        FieldTrip.Id = Id;
        FieldTripRequested?.Invoke(this, FieldTrip);
    }

    [RelayCommand]
    public async Task LoadFacilities()
    {
        Busy = true;
        if (string.IsNullOrEmpty(Facilites.Model.id) && !Facilites.HasLoaded)
        {
            var facilities = await _service.GetFacilitiesEvent(Id, OnError.DefaultBehavior(this));
            if (!facilities.HasValue)
            {
                Facilites.Clear();
                HasFacilities = false;
                Busy = false;
                return;
            }
            Facilites.Load(facilities.Value);
            HasFacilities = true;
        }
        Busy = false;
        FacilitesRequested?.Invoke(this, Facilites);
    }

    [RelayCommand]
    public async Task LoadTech()
    {
        Busy = true;
        if (string.IsNullOrEmpty(Tech.Model.id) && !Tech.HasLoaded)
        {
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
        }
        Busy = false;

        TechRequested?.Invoke(this, Tech);
    }

    [RelayCommand]
    public async Task LoadCatering()
    {
        Busy = true;
        if (string.IsNullOrEmpty(Catering.Model.id) && !Catering.HasLoaded)
        {
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
        }
        Busy = false;

        CateringRequested?.Invoke(this, Catering);
    }

    [RelayCommand]
    public async Task LoadTheater()
    {
        Busy = true;
        if (string.IsNullOrEmpty(Theater.Model.eventId) && !Theater.HasLoaded)
        {
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
        }
        Busy = false;
        TheaterRequested?.Invoke(this, Theater);
    }

    [RelayCommand]
    public async Task LoadFieldTrip()
    {
        Busy = true;

        if (string.IsNullOrEmpty(FieldTrip.Model.eventId) && !FieldTrip.HasLoaded && Model.Reduce(EventFormBase.Empty).hasFieldTripInfo)
        { 
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
        }

        Busy = false;
        FieldTripRequested?.Invoke(this, FieldTrip);
    }

    [RelayCommand]
    public async Task LoadMarComm()
    {
        Busy = true;
        if (string.IsNullOrEmpty(MarComm.Model.eventId) && !MarComm.HasLoaded)
        {
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
        }

        Busy = false;
        MarCommRequested?.Invoke(this, MarComm);
    }
    public static ConcurrentBag<EventFormViewModel> ViewModelCache { get; private set; } = [];

    public static void ResetCache(IEnumerable<EventFormBase> newCache)
    {
        ViewModelCache = [];
        _ = GetClonedViewModels(newCache);
    }

    public static EventFormViewModel Get(EventFormBase model)
    {
        var vm = ViewModelCache.FirstOrDefault(evt => evt.Model.Map(e => e.id == evt.Id).Reduce(false));
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
            StartDate = model.start,
            StartTime = TimeOnly.FromDateTime(model.start).ToTimeSpan(),
            EndDate = model.end,
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
            CanEditCatering = model.start > DateTime.Today.AddDays(14),
            Model = OptionalStruct<EventFormBase>.Some(model),
            UserIsAdmin = IsAdmin,
            UserIsRegistrar = IsRegistrar,
            IsPending = model.status == ApprovalStatusLabel.Pending,
            IsDeleted = model.status == ApprovalStatusLabel.Withdrawn || model.status == ApprovalStatusLabel.Declined,
            NeedsRoomApproval = model.status == ApprovalStatusLabel.RoomNotCleared
        };

        vm.CanEditBase = vm.IsCreating || vm.IsUpdating;
        vm.CanEditSubForms = vm.IsCreating || vm.IsUpdating;

        vm.StatusSelection.Select(eventForms.StatusLabels.First(status => status.label.Equals(model.status, StringComparison.InvariantCultureIgnoreCase)));
        vm.LeaderSearch.Select(UserViewModel.Get(registrar.AllUsers.First(u => u.id == model.leaderId)));

        vm.Leader = vm.LeaderSearch.Selected;
        
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
            vm.Attachments = new(model);
            vm.Attachments.OnError += (sender, e) => vm.OnError?.Invoke(sender, e);
        }

        return vm.Clone();
    }

    public static List<EventFormViewModel> GetClonedViewModels(IEnumerable<EventFormBase> models) => 
        models.Select(Get).ToList();

    public static async Task Initialize(EventFormsService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

        _ = GetClonedViewModels(await service.GetMyCreatedEvents(default, default, onError));
        _ = GetClonedViewModels(await service.GetMyLeadEvents(default, default, onError));
        ViewModelCache = [.. ViewModelCache.Distinct()];
    }

    public EventFormViewModel Clone() => 
        (EventFormViewModel)MemberwiseClone();

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
    [ObservableProperty] ObservableCollection<EventTypeViewModel> types = [];
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
            Types = [..EventTypeViewModel.GetClonedViewModels(service.EventTypes)];
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
        var vm = Types.FirstOrDefault(t => ((string)t.Type).Equals(type, StringComparison.InvariantCultureIgnoreCase));
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
    public EventType Type { get; init; }

    [ObservableProperty] bool isSelected;
    
    public static implicit operator string(EventTypeViewModel vm) => vm.Type;

    public override string ToString() => Type;

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
    [ObservableProperty] ObservableCollection<ApprovalStatusViewModel> statusList = [];
    [ObservableProperty] ApprovalStatusViewModel selected = new();
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool showList;

    public ApprovalStatusSelectionViewModel()
    {
        var service = ServiceHelper.GetService<EventFormsService>();
        var task = service.WaitForInit(OnError.DefaultBehavior(this));
        task.WhenCompleted(() =>
        {
            StatusList = [..ApprovalStatusViewModel.GetClonedViewModels(service.StatusLabels)];
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
            StatusList.Add(vm);
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
