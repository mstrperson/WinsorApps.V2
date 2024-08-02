using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;

public partial class CalendarDayViewModel :
    ObservableObject
{
    [ObservableProperty] ObservableCollection<AssessmentCalendarEventViewModel> events = [];
    [ObservableProperty] DateTime date;
    [ObservableProperty] string cycleDay = "";

    public EventHandler<AssessmentCalendarEventViewModel>? EventSelected;

    public CalendarDayViewModel()
    {
        
    }

    public static CalendarDayViewModel Get(DateTime date, IEnumerable<AssessmentCalendarEvent> events)
    {
        var vm = new CalendarDayViewModel()
        {
            Date = date,
            Events = [.. events.Where(evt => evt.start.Date == date.Date).Select(AssessmentCalendarEventViewModel.Get)]
        }; 
        
        var cycleDays = ServiceHelper.GetService<CycleDayCollection>();
        var cd = cycleDays[date];
        if (cd.HasValue)
            vm.CycleDay = cd.Value.cycleDay;

        foreach (var evt in vm.Events)
        {
            evt.Selected += (_, e) => vm.EventSelected?.Invoke(vm, e);
        }

        return vm;
    }
}

public partial class CalendarWeekViewModel :
    ObservableObject
{
    [ObservableProperty] DateTime monday;
    [ObservableProperty] ObservableCollection<CalendarDayViewModel> days = [];

    public EventHandler<AssessmentCalendarEventViewModel>? EventSelected;

    public static CalendarWeekViewModel Get(DateTime date, IEnumerable<AssessmentCalendarEvent> events)
    {
        var monday = date.MondayOf();

        DateTime[] week = [monday, monday.AddDays(1), monday.AddDays(2), monday.AddDays(3), monday.AddDays(4)];

        var vm = new CalendarWeekViewModel()
        {
            Monday = monday,
            Days = [.. week.Select(day => CalendarDayViewModel.Get(day, events))]
        };

        foreach (var day in vm.Days)
            day.EventSelected += (_, e) => vm.EventSelected?.Invoke(vm, e);

        return vm;
    }
}

public partial class CalendarMonthViewModel :
    ObservableObject,
    IErrorHandling
{
    private readonly CycleDayCollection _cycleDays = ServiceHelper.GetService<CycleDayCollection>();

    [ObservableProperty] DateTime month;
    [ObservableProperty] ObservableCollection<CalendarWeekViewModel> weeks = [];

    public event EventHandler<AssessmentCalendarEventViewModel>? EventSelected;
    public event EventHandler<ErrorRecord>? OnError;

    public static async Task<CalendarMonthViewModel> Get(DateTime date, Task<ImmutableArray<AssessmentCalendarEvent>> getEventsTask)
    {
        var events = await getEventsTask;
        return Get(date, events);
    }

    public static CalendarMonthViewModel Get(DateTime date, IEnumerable<AssessmentCalendarEvent> events)
    {
        var month = date.MonthOf();

        var monday = month.MondayOf();

        List<DateTime> weeks = [monday];
        for(var week = monday.AddDays(7); week.Month == date.Month; week = week.AddDays(7))
        {
            weeks.Add(week);
        }

        var vm = new CalendarMonthViewModel()
        {
            Month = month,
            Weeks = [.. weeks.Select(wk => CalendarWeekViewModel.Get(wk, events))]
        };

        foreach (var week in vm.Weeks)
            week.EventSelected += (_, e) => vm.EventSelected?.Invoke(vm, e);

        return vm;
    }

    public async Task IncrementMonth(Task<ImmutableArray<AssessmentCalendarEvent>> getEventsTask)
    {
        var nextMonth = Month.AddMonths(1);

        _ = await _cycleDays.GetCycleDays(DateOnly.FromDateTime(nextMonth), DateOnly.FromDateTime(nextMonth).AddMonths(1), OnError.DefaultBehavior(this));

        var events = await getEventsTask;

        var monday = nextMonth.MondayOf();

        List<DateTime> weeks = [monday];
        for (var week = monday.AddDays(7); week.Month == nextMonth.Month; week = week.AddDays(7))
        {
            weeks.Add(week);
        }

        Month = nextMonth;
        Weeks = [.. weeks.Select(wk => CalendarWeekViewModel.Get(wk, events))];

        foreach (var week in Weeks)
            week.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }

    public async Task DecrementMonth(Task<ImmutableArray<AssessmentCalendarEvent>> getEventsTask)
    {
        var nextMonth = Month.AddMonths(-1);

        _ = await _cycleDays.GetCycleDays(DateOnly.FromDateTime(nextMonth), DateOnly.FromDateTime(nextMonth).AddMonths(1), OnError.DefaultBehavior(this));

        var events = await getEventsTask;

        var monday = nextMonth.MondayOf();

        List<DateTime> weeks = [monday];
        for (var week = monday.AddDays(7); week.Month == nextMonth.Month; week = week.AddDays(7))
        {
            weeks.Add(week);
        }

        Month = nextMonth;
        Weeks = [.. weeks.Select(wk => CalendarWeekViewModel.Get(wk, events))];

        foreach (var week in Weeks)
            week.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }
}
