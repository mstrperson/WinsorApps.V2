using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global;

namespace WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;

public partial class CalendarDayViewModel :
    ObservableObject
{
    [ObservableProperty] ObservableCollection<AssessmentCalendarEventViewModel> events = [];
    [ObservableProperty] DateTime date;

    public static CalendarDayViewModel Get(DateTime date, IEnumerable<AssessmentCalendarEvent> events) => new()
    {
        Date = date,
        Events = [.. events.Where(evt => evt.start.Date == date.Date).Select(AssessmentCalendarEventViewModel.Get)]
    };
}

public partial class CalendarWeekViewModel :
    ObservableObject
{
    [ObservableProperty] DateTime monday;
    [ObservableProperty] ObservableCollection<CalendarDayViewModel> days;

    public static CalendarWeekViewModel Get(DateTime date, IEnumerable<AssessmentCalendarEvent> events)
    {
        var monday = date.MondayOf();

        DateTime[] week = [monday, monday.AddDays(1), monday.AddDays(2), monday.AddDays(3), monday.AddDays(4)];

        return new()
        {
            Monday = monday,
            Days = [.. week.Select(day => CalendarDayViewModel.Get(day, events))]
        };
    }
}

public partial class CalendarMonthViewModel :
    ObservableObject
{
    [ObservableProperty] DateTime month;
    [ObservableProperty] ObservableCollection<CalendarWeekViewModel> weeks = [];

    public static CalendarMonthViewModel Get(DateTime date, IEnumerable<AssessmentCalendarEvent> events)
    {
        var month = date.MonthOf();

        var monday = month.MondayOf();

        List<DateTime> weeks = [monday];
        for(var week = monday.AddDays(7); week.Month != date.Month; week = week.AddDays(7))
        {
            weeks.Add(week);
        }

        return new()
        {
            Month = month,
            Weeks = [.. weeks.Select(wk => CalendarWeekViewModel.Get(wk, events))]
        };
    }
}
