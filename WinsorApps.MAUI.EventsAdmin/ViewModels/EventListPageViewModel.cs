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

namespace WinsorApps.MAUI.EventsAdmin.ViewModels;

public partial class EventListPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    public event EventHandler<ErrorRecord>? OnError;

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

    [ObservableProperty] bool isAdmin;
    [ObservableProperty] bool isRegistrar;
    public EventListPageViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        isAdmin = registrar.MyRoles.Intersect(["System Admin", "Winsor - Events Admin"]).Any();
        isRegistrar = registrar.MyRoles.Intersect(["System Admin", "Registrar"]).Any();
    }

    public EventListPageViewModel(IEnumerable<EventFormBase> events)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        isAdmin = registrar.MyRoles.Intersect(["System Admin", "Winsor - Events Admin"]).Any();
        isRegistrar = registrar.MyRoles.Intersect(["System Admin", "Registrar"]).Any();
        AllEvents = [.. events.OrderBy(evt => evt.start)]; 
        
        PendingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Pending)];
        ApprovedEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Approved)];
        WaitingEvents = [.. AllEvents.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.RoomNotCleared)];
        string[] states = [ApprovalStatusLabel.Pending, ApprovalStatusLabel.Approved, ApprovalStatusLabel.RoomNotCleared];
        OtherEvents = [.. AllEvents.Where(evt => !states.Contains(evt.Form.StatusSelection.Selected.Label))];

        foreach (var evt in AllEvents)
        {
            evt.OnError += (sender, e) => OnError?.Invoke(sender, e);
            evt.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            evt.StatusChanged += (_, _) =>
            {
                List<AdminFormViewModel> events = [.. PendingEvents, .. ApprovedEvents, .. WaitingEvents, .. OtherEvents];

                PendingEvents = [.. events.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Pending)];
                ApprovedEvents = [.. events.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.Approved)];
                WaitingEvents = [.. events.Where(evt => evt.Form.StatusSelection.Selected.Label == ApprovalStatusLabel.RoomNotCleared)];
                string[] states = [ApprovalStatusLabel.Pending, ApprovalStatusLabel.Approved, ApprovalStatusLabel.RoomNotCleared];
                OtherEvents = [.. events.Where(evt => !states.Contains(evt.Form.StatusSelection.Selected.Label))];
            };
        }
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
