using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.EventForms.Services.Admin;
using System.Collections.Immutable;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels;

public partial class EventListPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AdminFormViewModel>? FormSelected;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] ObservableCollection<AdminFormViewModel> pendingEvents = [];
    [ObservableProperty] bool showPending = true;
    [ObservableProperty] ObservableCollection<AdminFormViewModel> approvedEvents = [];
    [ObservableProperty] bool showApproved;
    [ObservableProperty] ObservableCollection<AdminFormViewModel> waitingEvents = [];
    [ObservableProperty] bool showWaiting;
    [ObservableProperty] ObservableCollection<AdminFormViewModel> otherEvents = [];
    [ObservableProperty] bool showOther;
    [ObservableProperty] ObservableCollection<AdminFormViewModel> allEvents = [];
    [ObservableProperty] bool showAll;

    [ObservableProperty] DateTime start;
    [ObservableProperty] DateTime end;
    [ObservableProperty] ObservableCollection<AdminFormViewModel> twoWeekList = [];

    [ObservableProperty] bool isAdmin;
    [ObservableProperty] bool isRegistrar;
    public EventListPageViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        isAdmin = registrar.MyRoles.Intersect(["System Admin", "Winsor - Events Admin"]).Any();
        isRegistrar = registrar.MyRoles.Intersect(["System Admin", "Registrar"]).Any();
        Start = DateTime.Today;
        End = Start.AddDays(14);
    }

    private readonly EventsAdminService _admin = ServiceHelper.GetService<EventsAdminService>();
    public async Task Initialize(ErrorAction onError)
    {
        await _admin.WaitForInit(onError);

        LoadEvents([.. _admin.AllEvents.Values]);
        _admin.OnCacheRefreshed += (_, _) => 
            LoadEvents([.. _admin.AllEvents.Values]);
    }

    public void LoadEvents(ImmutableArray<EventFormBase> events)
    {
        AllEvents = [.. events.OrderBy(evt => evt.start)]; 
        
        TwoWeekList = [.. AllEvents.Where(evt => 
               evt.Form.StartDate >= Start 
            && evt.Form.EndDate <= End
            && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Withdrawn
            && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Declined)];
        
        PendingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Pending)];
        ApprovedEvents = [.. TwoWeekList.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Approved)];
        WaitingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.RoomNotCleared)];
        OtherEvents = [.. TwoWeekList.Where(evt => !SpecificStates.Contains(evt.Form.StatusSelection.Selected.Label))];

        foreach (var evt in AllEvents)
        {
            evt.OnError += (sender, e) => OnError?.Invoke(sender, e);
            evt.Selected += (_, _) => FormSelected?.Invoke(this, evt);
            evt.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            evt.StatusChanged += (_, _) =>
            {
                TwoWeekList = [.. AllEvents.Where(evt => evt.Form.StartDate >= Start && evt.Form.EndDate <= End)];

                PendingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Pending)];
                ApprovedEvents = [.. TwoWeekList.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Approved)];
                WaitingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.RoomNotCleared)];
                OtherEvents = [.. TwoWeekList.Where(evt => !SpecificStates.Contains(evt.Form.StatusSelection.Selected.Label))];
            };
        }
    }

    private static string[] SpecificStates = [ApprovalStatusLabel.Pending, ApprovalStatusLabel.Approved, ApprovalStatusLabel.RoomNotCleared];

    [RelayCommand]
    public void NextPage()
    {
        Start = Start.AddDays(14);
        End = Start.AddDays(14);
        ReloadLists();
    }

    private void ReloadLists()
    {
        TwoWeekList = [.. AllEvents.Where(evt =>
               evt.Form.StartDate >= Start
            && evt.Form.EndDate <= End
            && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Withdrawn
            && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Declined)];
        ApprovedEvents = [.. TwoWeekList.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Approved)];
        OtherEvents = [.. TwoWeekList.Where(evt => !SpecificStates.Contains(evt.Form.StatusSelection.Selected.Label))];
    }

    [RelayCommand]
    public void PreviousPage()
    {
        Start = Start.AddDays(-14);
        End = Start.AddDays(14);
        ReloadLists();
    }

    [RelayCommand]
    public void ToggleShowPending()
    {
        ShowPending = true;
        ShowApproved = false;
        ShowWaiting = false;
        ShowOther = false;
        ShowAll = false;
    }
    [RelayCommand]
    public void ToggleShowApproved()
    {
        ShowPending = false;
        ShowApproved = true;
        ShowWaiting = false;
        ShowOther = false;
        ShowAll = false;
    }
    [RelayCommand]
    public void ToggleShowWaiting()
    {
        ShowPending = false;
        ShowApproved = false;
        ShowWaiting = true;
        ShowOther = false;
        ShowAll = false;
    }
    [RelayCommand]
    public void ToggleShowOther()
    {
        ShowPending = false;
        ShowApproved = false;
        ShowWaiting = false;
        ShowOther = true;
        ShowAll = false;
    }
    [RelayCommand]
    public void ToggleShowAll()
    {
        ShowPending = false;
        ShowApproved = false;
        ShowWaiting = false;
        ShowOther = false;
        ShowAll = true;
    }
}
