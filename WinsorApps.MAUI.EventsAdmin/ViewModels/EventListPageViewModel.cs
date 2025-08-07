using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.EventForms.Services.Admin;
using System.Collections.Immutable;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.Services.Global;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels;

public partial class EventListPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AdminFormViewModel>? FormSelected;

    [ObservableProperty] private bool busy = true;
    [ObservableProperty] private string busyMessage = "Initializing...";

    [ObservableProperty] private ObservableCollection<AdminFormViewModel> pendingEvents = [];
    [ObservableProperty] private double pendingHeight;
    [ObservableProperty] private bool showPending = true;
    [ObservableProperty] private ObservableCollection<AdminFormViewModel> waitingEvents = [];
    [ObservableProperty] private double waitingHeight;
    [ObservableProperty] private bool showWaiting;
    [ObservableProperty] private ObservableCollection<AdminFormViewModel> otherEvents = [];
    [ObservableProperty] private double otherHeight;
    [ObservableProperty] private bool showOther;
    [ObservableProperty] private ObservableCollection<AdminFormViewModel> allEvents = [];
    [ObservableProperty] private bool showAll;

    [ObservableProperty] private DateTime start;
    [ObservableProperty] private bool showMonthSelector;
    [ObservableProperty] private ObservableCollection<SelectableViewModel<DateTime>> months = 
    [.. 
        new DateRange(DateTime.Today.MonthOf().AddYears(-1), DateTime.Today.MonthOf().AddYears(1))
        .Where(dt => dt.Day == 1)
        .Select(dt => dt.ToDateTime(default))
    ];
    [ObservableProperty] private DateTime end;
    [ObservableProperty] private ObservableCollection<AdminFormViewModel> twoWeekList = [];
    [ObservableProperty] private double twoWeekHeight;

    [ObservableProperty] private bool isAdmin;
    [ObservableProperty] private bool isRegistrar;

    [ObservableProperty] private bool hasLoaded;
    
    private static readonly double _headerHeight = 150;
    private static readonly double _rowHeight = 80;

    [RelayCommand]
    public async Task ToggleMonthSelector()
    {
        ShowMonthSelector = !ShowMonthSelector;
        if(!ShowMonthSelector)
        {
            Start = Start.MonthOf();
            End = Start.AddMonths(1);
            if (End > _admin.CacheEndDate || Start < _admin.CacheStartDate)
                _ = await _admin.GetAllEvents(OnError.DefaultBehavior(this), Start, End);
            await ReloadLists();
        }
    }


    public EventListPageViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        isAdmin = registrar.MyRoles.Intersect(["System Admin", "Winsor - Events Admin"]).Any();
        isRegistrar = registrar.MyRoles.Intersect(["System Admin", "Registrar"]).Any();
        Start = DateTime.Today;
        End = Start.AddDays(14);
    }

    private readonly EventsAdminService _admin = ServiceHelper.GetService<EventsAdminService>();
    private readonly EventFormViewModelCacheService _cacheService = ServiceHelper.GetService<EventFormViewModelCacheService>();
    public async Task Initialize(ErrorAction onError, bool reload = false)
    {
        if (!reload && HasLoaded)
            return;
        Busy = true;
        BusyMessage= "Initializing...";
        await _admin.WaitForInit(onError);

        LoadEvents([.. _admin.AllEvents.Where(evt =>
            evt.status == ApprovalStatusLabel.Pending || evt.status == ApprovalStatusLabel.RoomNotCleared ||
            evt.start.MonthOf() == DateTime.Today.MonthOf())]);
        _admin.OnCacheRefreshed += (_, _) => 
            LoadEvents([.. _admin.AllEvents]);
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Refreshing Events...";
        _admin.ClearCache();
        await _admin.ForceDelta(OnError.DefaultBehavior(this), DateTime.Today.AddDays(-7));
        await _cacheService.Refresh(OnError.DefaultBehavior(this));
        LoadEvents([.. _admin.AllEvents]);
        Busy = false;
    }
    
    public void LoadEvents(List<EventFormBase> events)
    {
        Busy = true;
        BusyMessage = "Loading Events.";

        AllEvents = [.. events.OrderBy(evt => evt.start).Select(_cacheService.Get)];
        TwoWeekList = [.. AllEvents.Where(evt =>
               evt.Form.StartDate >= Start
            && evt.Form.EndDate <= End
            && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Withdrawn
            && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Declined)];
        TwoWeekHeight = _headerHeight + (_rowHeight * TwoWeekList.Count);

        PendingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Pending)];
        PendingHeight = _headerHeight + (_rowHeight * PendingEvents.Count);
        WaitingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.RoomNotCleared)];
        WaitingHeight = _headerHeight + (_rowHeight * WaitingEvents.Count);
        OtherEvents = [.. TwoWeekList.Where(evt => !SpecificStates.Contains(evt.Form.StatusSelection.Selected.Label))];
        OtherHeight = _headerHeight + (_rowHeight * OtherEvents.Count);
        ConnectEvents();

        Busy = false;
        HasLoaded = true;
    }

    private void ConnectEvents()
    {
        foreach (var evt in AllEvents)
        {
            evt.OnError += (sender, e) => OnError?.Invoke(sender, e);
            evt.Selected += (_, _) => FormSelected?.Invoke(this, evt);
            evt.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            evt.StatusChanged += (_, _) =>
            {
                TwoWeekList = [.. AllEvents.Where(evt =>
                    evt.Form.StartDate >= Start
                    && evt.Form.EndDate <= End
                    && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Withdrawn
                    && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Declined)];
                TwoWeekHeight = _headerHeight + (_rowHeight * TwoWeekList.Count);

                PendingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Pending)];
                PendingHeight = _headerHeight + (_rowHeight * PendingEvents.Count);
                WaitingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.RoomNotCleared)];
                WaitingHeight = _headerHeight + (_rowHeight * WaitingEvents.Count);
                OtherEvents = [.. TwoWeekList.Where(evt => !SpecificStates.Contains(evt.Form.StatusSelection.Selected.Label))];
                OtherHeight = _headerHeight + (_rowHeight * OtherEvents.Count);
            };
        }
    }

    private static readonly string[] SpecificStates = [ApprovalStatusLabel.Pending, ApprovalStatusLabel.Approved, ApprovalStatusLabel.RoomNotCleared];

    [RelayCommand]
    public async Task NextPage()
    {
        Busy = true;
        BusyMessage = "Loading Events...";
        Start = Start.AddMonths(1);
        End = Start.AddMonths(1);
        if (End > _admin.CacheEndDate)
            _ = await _admin.GetAllEvents(OnError.DefaultBehavior(this), Start, End);
        
        await ReloadLists();

        Busy = false;
    }

    private async Task ReloadLists() => await Task.Run(() =>
    {
        Busy = true;
        BusyMessage = "Loading Events...";
        AllEvents = [.. _admin.AllEvents.OrderBy(evt => evt.start).Select(_cacheService.Get)];
        TwoWeekList =
        [
            .. AllEvents.Where(evt =>
                evt.Form.StartDate >= Start
                && evt.Form.EndDate <= End
                && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Withdrawn
                && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Declined)
        ];
        TwoWeekHeight = _headerHeight + (_rowHeight * TwoWeekList.Count);
        OtherEvents = [.. TwoWeekList.Where(evt => !SpecificStates.Contains(evt.Form.StatusSelection.Selected.Label))];
        OtherHeight = _headerHeight + (_rowHeight * OtherEvents.Count);

        ConnectEvents();

        Busy = false;

    });

    [RelayCommand]
    public async Task PreviousPage()
    {
        Busy = true;
        BusyMessage = "Loading Events";
        Start = Start.AddMonths(-1);
        End = Start.AddMonths(1);
        if (Start < _admin.CacheStartDate)
            _ = await _admin.GetAllEvents(OnError.DefaultBehavior(this), Start, End);
        
        await ReloadLists();
        Busy = false;
    }

    [RelayCommand]
    public void ToggleShowPending()
    {
        ShowPending = true;
        ShowWaiting = false;
        ShowOther = false;
        ShowAll = false;
    }
    [RelayCommand]
    public void ToggleShowApproved()
    {
        ShowPending = false;
        ShowWaiting = false;
        ShowOther = false;
        ShowAll = false;
    }
    [RelayCommand]
    public void ToggleShowWaiting()
    {
        ShowPending = false;
        ShowWaiting = true;
        ShowOther = false;
        ShowAll = false;
    }
    [RelayCommand]
    public void ToggleShowOther()
    {
        ShowPending = false;
        ShowWaiting = false;
        ShowOther = true;
        ShowAll = false;
    }
    [RelayCommand]
    public void ToggleShowAll()
    {
        ShowPending = false;
        ShowWaiting = false;
        ShowOther = false;
        ShowAll = true;
    }
}
