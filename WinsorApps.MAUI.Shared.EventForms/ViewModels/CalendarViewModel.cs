using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class CalendarViewModel :
    ObservableObject
{
    [ObservableProperty] ObservableCollection<CalendarWeekViewModel> weeks = [];
    [ObservableProperty] DateTime month;

    /// <summary>
    /// Event source that returns a collection of events that are in the provided Month. 
    /// </summary>
    public required Func<DateTime, IEnumerable<EventFormBase>> MonthlyEventSource;

    public void ApplyFilter(Func<EventFormViewModel, bool> filter)
    {
        foreach(var week in Weeks)
            week.ApplyFilter(filter);
    }

    [RelayCommand]
    public void LoadEvents()
    {
        var events = EventFormViewModel.GetClonedViewModels(MonthlyEventSource(Month));
        var firstCalendarDay = new DateTime(Month.Year, Month.Month, 1).MondayOf().AddDays(-1);
        for(DateTime sunday = firstCalendarDay; sunday.Month <= Month.Month; sunday = sunday.AddDays(7))
        {
            Weeks.Add(new(sunday, events));
        }
    }

    [RelayCommand]
    public void IncrementMonth()
    {
        Month = Month.AddMonths(1);
        LoadEvents();
    }

    [RelayCommand]
    public void DecrementMonth()
    {
        Month = Month.AddMonths(-1);
        LoadEvents();
    }
}

public partial class CalendarWeekViewModel :
    ObservableObject
{
    [ObservableProperty] ObservableCollection<CalendarDayViewModel> days = [];

    public CalendarWeekViewModel(DateTime sunday, List<EventFormViewModel> events)
    {
        for (DateTime date = sunday; date.DayOfYear < sunday.DayOfYear + 7; date = date.AddDays(1))
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
    [ObservableProperty] DateTime date;
    [ObservableProperty] ObservableCollection<EventFormViewModel> events = [];
    [ObservableProperty] ObservableCollection<EventFormViewModel> filteredEvents = [];

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
