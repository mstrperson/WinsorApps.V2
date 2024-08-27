using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class MonthlyCalendarViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly ReadonlyCalendarService _service;
    private readonly LocalLoggingService _logging;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AssessmentCalendarEventViewModel>? EventSelected;
    public event EventHandler<CalendarDayViewModel>? DaySelected;

    [ObservableProperty] CalendarMonthViewModel calendar = new();
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    public MonthlyCalendarViewModel(ReadonlyCalendarService service, LocalLoggingService logging)
    {
        _service = service;
        _logging = logging;
    }

    public async Task Initialize(ErrorAction onError)
    {
        Busy = true;
        BusyMessage = "Initializing";
        await _service.WaitForInit(onError);
        var today = DateTime.Today.MonthOf();
        Calendar = await CalendarMonthViewModel.Get(today, 
            _service.GetAssessmentCalendarInRange(onError, DateOnly.FromDateTime(today), 
                DateOnly.FromDateTime(today.AddMonths(1))));
        Calendar.GetEventsTask = (date) => _service.GetAssessmentCalendarInRange(onError, DateOnly.FromDateTime(date), DateOnly.FromDateTime(date.AddMonths(1)));
        Calendar.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
        Calendar.DaySelected += (_, day) => DaySelected?.Invoke(this, day);
        Busy = false;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Refreshing Calendar";
        Calendar = await CalendarMonthViewModel.Get(Calendar.Month, 
            _service.GetAssessmentCalendarInRange(OnError.DefaultBehavior(this), DateOnly.FromDateTime(Calendar.Month), 
                DateOnly.FromDateTime(Calendar.Month.AddMonths(1))));
        Calendar.GetEventsTask = (date) => _service.GetAssessmentCalendarInRange(OnError.DefaultBehavior(this), DateOnly.FromDateTime(date), DateOnly.FromDateTime(date.AddMonths(1)));
        Calendar.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
        Calendar.DaySelected += (_, day) => DaySelected?.Invoke(this, day);
        Busy = false;
    }

    [RelayCommand]
    public async Task IncrementMonth()
    {
        Busy = true;
        BusyMessage = "Loading Next Month's Assessments";
        await Calendar.IncrementMonth();
        Busy = false;
    }

    [RelayCommand]
    public async Task DecrementMonth()
    {
        Busy = true;
        BusyMessage = "Loading Previous Month's Assessments";
        await Calendar.DecrementMonth();
        Busy = false;
    }
}
