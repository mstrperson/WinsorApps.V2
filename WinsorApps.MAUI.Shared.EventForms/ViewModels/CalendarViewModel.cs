﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class CalendarViewModel :
    ObservableObject,
    IBusyViewModel
{
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] private ObservableCollection<CalendarWeekViewModel> weeks = [];
    [ObservableProperty] private DateTime month = DateTime.Today.MonthOf();
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";
    
    [ObservableProperty] private bool hasLoaded;

    public event EventHandler<EventFormViewModel>? EventSelected;

    /// <summary>
    /// Event source that returns a collection of events that are in the provided Month. 
    /// </summary>
    public required Func<DateTime, IEnumerable<EventFormBase>> MonthlyEventSource;

    public required Func<IEnumerable<EventFormBase>, List<EventFormViewModel>> ViewModelFactory { get; set; } = EventFormViewModel.GetClonedViewModels;
    
    public void ApplyFilter(Func<EventFormViewModel, bool> filter)
    {
        foreach(var week in Weeks)
            week.ApplyFilter(filter);
    }

    [RelayCommand]
    public async Task LoadEvents() => await Task.Run(() =>
    {
        Busy = true;
        BusyMessage = $"Loading Calendar Events for {Month:MMMM yyyy}";
        using DebugTimer _ = new(BusyMessage, _logging);
        var events = ViewModelFactory(MonthlyEventSource(Month));
        foreach (var evt in events)
            evt.Selected += (_, _) => 
                EventSelected?.Invoke(this, evt);

        Weeks = [];
        var firstCalendarDay = new DateTime(Month.Year, Month.Month, 1).MondayOf().AddDays(-1);
        for (var sunday = firstCalendarDay; 100 * sunday.Year + sunday.Month <= 100 * Month.Year + Month.Month; sunday = sunday.AddDays(7))
        {
            Weeks.Add(new(sunday, events));
        }

        Busy = false;
    });

    [RelayCommand]
    public async Task IncrementMonth()
    {
        Month = Month.AddMonths(1);
        await LoadEvents();
    }

    [RelayCommand]
    public async Task DecrementMonth()
    {
        Month = Month.AddMonths(-1);
        await LoadEvents();
    }
}

public partial class CalendarWeekViewModel :
    ObservableObject
{
    [ObservableProperty] private ObservableCollection<CalendarDayViewModel> days = [];

    public CalendarWeekViewModel(DateTime sunday, List<EventFormViewModel> events)
    {
        var nextSunday = sunday.AddDays(7);
        for (var date = sunday; date < nextSunday; date = date.AddDays(1))
        {
            Days.Add(new(date, events));
        }
    }

    public void ApplyFilter(Func<EventFormViewModel, bool> filter)
    {
        foreach (var day in Days)
            day.ApplyFilter(filter);
    }
}

public partial class CalendarDayViewModel :
    ObservableObject
{
    [ObservableProperty] private DateTime date;
    [ObservableProperty] private ObservableCollection<EventFormViewModel> events = [];
    [ObservableProperty] private ObservableCollection<EventFormViewModel> filteredEvents = [];

    public CalendarDayViewModel(DateTime date, List<EventFormViewModel> events)
    {
        Date = date;
        Events = [.. events.Where(evt => evt.StartDate.Date == date.Date)];
        FilteredEvents = [.. Events];
    }

    public void ApplyFilter(Func<EventFormViewModel, bool> filter)
    {
        FilteredEvents = [.. Events.Where(filter)];
    }
}
