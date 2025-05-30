﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
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
    [ObservableProperty] double pendingHeight;
    [ObservableProperty] bool showPending = true;
    [ObservableProperty] ObservableCollection<AdminFormViewModel> waitingEvents = [];
    [ObservableProperty] double waitingHeight;
    [ObservableProperty] bool showWaiting;
    [ObservableProperty] ObservableCollection<AdminFormViewModel> otherEvents = [];
    [ObservableProperty] double otherHeight;
    [ObservableProperty] bool showOther;
    [ObservableProperty] ObservableCollection<AdminFormViewModel> allEvents = [];
    [ObservableProperty] bool showAll;

    [ObservableProperty] DateTime start;
    [ObservableProperty] DateTime end;
    [ObservableProperty] ObservableCollection<AdminFormViewModel> twoWeekList = [];
    [ObservableProperty] double twoWeekHeight;

    [ObservableProperty] bool isAdmin;
    [ObservableProperty] bool isRegistrar;

    private static readonly double _headerHeight = 150;
    private static readonly double _rowHeight = 40;
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

        LoadEvents([.. _admin.AllEvents]);
        _admin.OnCacheRefreshed += (_, _) => 
            LoadEvents([.. _admin.AllEvents]);
    }

    public void LoadEvents(List<EventFormBase> events)
    {
        Busy = true;
        BusyMessage = "Loading Events.";

        var diff = events.Where(evt => AllEvents.All(af => !af.Form.Model.MapStruct(e => e.IsSameAs(evt)).Reduce(true))).ToList();
        var updates = AllEvents.Where(af => diff.Any(evt => af.Form.Model.MapStruct(e => e.id == evt.id).Reduce(false))).ToList();


        AllEvents = [..  AllEvents.Except(updates), .. diff]; 
        AllEvents = [.. AllEvents.OrderBy(evt => evt.Form.StartDate) ];
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

        Busy = false;
    }

    private static readonly string[] SpecificStates = [ApprovalStatusLabel.Pending, ApprovalStatusLabel.Approved, ApprovalStatusLabel.RoomNotCleared];

    [RelayCommand]
    public async Task NextPage()
    {
        Busy = true;
        BusyMessage = "Loading Events...";
        Start = Start.AddDays(14);
        End = Start.AddDays(14);
        if (End > _admin.CacheEndDate)
            _ = await _admin.GetAllEvents(OnError.DefaultBehavior(this), Start, End);
        else
            ReloadLists();

        Busy = false;
    }

    private void ReloadLists()
    {
        Busy = true;
        BusyMessage = "Loading Events...";
        TwoWeekList = [.. AllEvents.Where(evt =>
               evt.Form.StartDate >= Start
            && evt.Form.EndDate <= End
            && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Withdrawn
            && evt.Form.StatusSelection.Selected.Label != ApprovalStatusLabel.Declined)];
        TwoWeekHeight = _headerHeight + (_rowHeight * TwoWeekList.Count);
        OtherEvents = [.. TwoWeekList.Where(evt => !SpecificStates.Contains(evt.Form.StatusSelection.Selected.Label))];
        OtherHeight = _headerHeight + (_rowHeight * OtherEvents.Count);
        Busy = false;
    }

    [RelayCommand]
    public async Task PreviousPage()
    {
        Busy = true;
        BusyMessage = "Loading Events";
        Start = Start.AddDays(-14);
        End = Start.AddDays(14);
        if (Start < _admin.CacheStartDate)
            _ = await _admin.GetAllEvents(OnError.DefaultBehavior(this), Start, End);
        else
            ReloadLists();
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
