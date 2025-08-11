using System.Diagnostics;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels;

public class EventFormViewModelCacheService : IAsyncInitService
{
    private readonly ReadonlyCalendarService _calendar;    
    private readonly RegistrarService _registrar;
    private readonly LocalLoggingService _logging;
    
    public EventFormViewModelCacheService(ReadonlyCalendarService calendar, RegistrarService registrar, LocalLoggingService logging)
    {
        _calendar = calendar;
        _registrar = registrar;
        _logging = logging;

        _calendar.OnCacheRefreshed += (_, _) =>
        {
            using DebugTimer asjdklf = new("Refreshing Event Form View Model Cache", _logging);
            _ = _calendar.EventForms.Select(m => Get(m.details));
        };
    }

    private bool IsAdmin => _registrar.MyRoles.Intersect(["Winsor - Events Admin", "System Admin"]).Any();

    private bool IsRegistrar => _registrar.MyRoles.Intersect(["Registrar", "System Admin"]).Any();


    public string CacheFileName => "";
    public async Task SaveCache() => await Task.CompletedTask;
    
    public List<EventFormViewModel> ViewModelCache { get; private set; } = [];
    
    public void ClearCache()
    {
    }

    public bool LoadCache() => false;

    public async Task Initialize(ErrorAction onError)
    {
        await _calendar.Initialize(onError);

        Started = true;
        Progress = 0;
        
        ViewModelCache = 
        [ .. 
            _calendar.EventForms
                .Where(evt => evt.start.MonthOf() == DateTime.Today.MonthOf())
                .Select(model =>
                {
                    var vm = Get(model.details);
                    Progress += 1.0/(_calendar.EventForms.Count);
                    return vm;
                })
        ];
            

        Progress = 1;
        Ready = true;
    }

    public EventFormViewModel Get(EventFormBase model)
    {
        using DebugTimer _ = new($"Loading Event Form View Model for {model.id}", _logging);
        var vm = ViewModelCache.FirstOrDefault(evt =>  model.id == evt.Id);
        if (vm is not null && vm.Model.Reduce(EventFormBase.Empty).IsSameAs(model))
        {
            return vm.Clone();
        }

        if (vm is not null)
        {
            Debug.WriteLine($"{vm.Id} has been updated, creating new view model instance.");
            ViewModelCache.Remove(vm);
        }

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
            Creator = UserViewModel.Get(registrar.AllUsers.FirstOrDefault(u => u.id == model.creatorId) ?? UserRecord.Empty),
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
            Model = Optional<EventFormBase>.Some(model),
            UserIsAdmin = IsAdmin,
            UserIsRegistrar = IsRegistrar,
            IsPending = model.status == ApprovalStatusLabel.Pending,
            IsDeleted = model.status == ApprovalStatusLabel.Withdrawn || model.status == ApprovalStatusLabel.Declined,
            NeedsRoomApproval = model.status == ApprovalStatusLabel.RoomNotCleared
        };

        vm.CanEditBase = vm.IsCreating || vm.IsUpdating;
        vm.CanEditSubForms = vm.IsCreating || vm.IsUpdating;

        vm.StatusSelection.Select(eventForms.StatusLabels.FirstOrDefault(status => status.label.Equals(model.status, StringComparison.InvariantCultureIgnoreCase)) ?? new ApprovalStatus("", "Unknown"));
        vm.LeaderSearch.Select(UserViewModel.Get(registrar.AllUsers.FirstOrDefault(u => u.id == model.leaderId) ?? UserRecord.Empty));

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

        if (model.attachments is not null)
        {
            vm.Attachments = new(model);
        }

        ViewModelCache.Add(vm);
        return vm.Clone();
    }

    public async Task WaitForInit(ErrorAction onError) => await Task.CompletedTask;

    public async Task Refresh(ErrorAction onError) => await Task.CompletedTask;

    public bool Started { get; private set; }
    public bool Ready { get; private set; }
    public double Progress { get; private set; }
}