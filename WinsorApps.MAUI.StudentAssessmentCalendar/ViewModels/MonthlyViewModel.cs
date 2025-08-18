using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;

public partial class MonthlyViewModel :
    ObservableObject,
    IErrorHandling
{
    private readonly StudentAssessmentService _service;
    private readonly LocalLoggingService _logging;
    private readonly CycleDayCollection _cycleDays;

    [ObservableProperty] private CalendarMonthViewModel calendar;

    public event EventHandler<AssessmentCalendarEventViewModel>? EventSelected;

    public MonthlyViewModel(StudentAssessmentService service, LocalLoggingService logging, CycleDayCollection cycleDays)
    {
        _service = service;
        _logging = logging;
        _cycleDays = cycleDays;

        calendar = CalendarMonthViewModel.Get(DateTime.Today.MonthOf(), _service.MyCalendar);
        Calendar.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }

    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public async Task IncrementMonth()
    {
        var nextMonth = Calendar.Month.AddMonths(1);

        _ = await _cycleDays.GetCycleDays(DateOnly.FromDateTime(nextMonth), DateOnly.FromDateTime(nextMonth).AddMonths(1), OnError.DefaultBehavior(this));

        _ = await _service.GetMyCalendarInRange(OnError.DefaultBehavior(this), nextMonth, nextMonth.AddMonths(1));

        Calendar = CalendarMonthViewModel.Get(nextMonth, _service.MyCalendar);
        Calendar.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }

    [RelayCommand]
    public async Task DecrementMonth()
    {
        var nextMonth = Calendar.Month.AddMonths(-1);

        _ = await _cycleDays.GetCycleDays(DateOnly.FromDateTime(nextMonth),DateOnly.FromDateTime(nextMonth).AddMonths(1), OnError.DefaultBehavior(this));

        _ = await _service.GetMyCalendarInRange(OnError.DefaultBehavior(this), nextMonth, nextMonth.AddMonths(1));

        Calendar = CalendarMonthViewModel.Get(nextMonth, _service.MyCalendar);
        Calendar.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }

    [RelayCommand]
    public async Task Refresh()
    {

        _ = await _service.GetMyCalendarInRange(OnError.DefaultBehavior(this), Calendar.Month, Calendar.Month.AddMonths(1));

        Calendar = CalendarMonthViewModel.Get(Calendar.Month, _service.MyCalendar);
        Calendar.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }
}
