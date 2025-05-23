﻿using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services.Admin;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels;

public partial class AdminCalendarViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly EventsAdminService _admin = ServiceHelper.GetService<EventsAdminService>();
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] CalendarViewModel calendar;
    [ObservableProperty] EventFilterViewModel eventFilterViewModel = new();
    [ObservableProperty] bool showFilter;
    
    [ObservableProperty] EventFormViewModel selectedEvent = new();
    [ObservableProperty] bool showEvent;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;

    public AdminCalendarViewModel()
    {
        calendar = new()
        {
            Month = DateTime.Today.MonthOf(),
            MonthlyEventSource = (month) =>
            {
                using DebugTimer _ = new($"Filtering Events from {month:MMMM yyyy}", _logging);
                return _admin.AllEvents.Where(evt =>
                        evt.start.Month == month.Month
                        && evt.status != ApprovalStatusLabel.Creating
                        && evt.status != ApprovalStatusLabel.Updating
                        && evt.status != ApprovalStatusLabel.Withdrawn
                        && evt.status != ApprovalStatusLabel.Declined)
                    .ToList();
            }
        };
        Calendar.EventSelected += (_, evt) =>
        {
            SelectedEvent = evt;
            ShowEvent = true;
        };
        _admin.OnCacheRefreshed += async (_, _) =>
        {
            Busy = true;
            BusyMessage = "Refreshing Event List";
            await Calendar.LoadEvents();
            ApplyFilter();
            Busy = false;
        };
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Loading Events";
        await Calendar.LoadEvents();
        ApplyFilter();
        Busy = false;
    }

    [RelayCommand]
    public void CloseSelectedEvent()
    {
        ShowEvent = false;
    }

    [RelayCommand]
    public async Task ToggleShowFilter()
    {
        ShowFilter = !ShowFilter;
        if (!ShowFilter)
        {
            Busy = true;
            BusyMessage = "Clearing Filter";
            EventFilterViewModel.ClearFilter();
            await Calendar.LoadEvents();
            Busy = false;
        }
    }

    [RelayCommand]
    public void ApplyFilter()
    {
        Calendar.ApplyFilter(EventFilterViewModel.Filter);
    }

    [RelayCommand]
    public async Task IncrementMonth()
    {
        Busy = true;
        BusyMessage = "Loading Next Month";
        await Calendar.IncrementMonth();
        if (Calendar.Month < _admin.CacheStartDate || Calendar.Month.AddMonths(1).AddDays(-1) > _admin.CacheEndDate)
        {
            BusyMessage = $"Downloading Event Data for {Calendar.Month:MMMM yyyy}";
            _ = await _admin.GetAllEvents(OnError.DefaultBehavior(this), Calendar.Month, Calendar.Month.AddMonths(1));
        }
        Busy = false;
    }


    [RelayCommand]
    public async Task DecrementMonth()
    {
        Busy = true;
        BusyMessage = "Loading Previous Month";
        await Calendar.DecrementMonth();
        if (Calendar.Month < _admin.CacheStartDate || Calendar.Month.AddMonths(1).AddDays(-1) > _admin.CacheEndDate)
        {
            BusyMessage = $"Downloading Event Data for {Calendar.Month:MMMM yyyy}";
            _ = await _admin.GetAllEvents(OnError.DefaultBehavior(this), Calendar.Month, Calendar.Month.AddMonths(1)); 
        }
        Busy = false;
    }
}
