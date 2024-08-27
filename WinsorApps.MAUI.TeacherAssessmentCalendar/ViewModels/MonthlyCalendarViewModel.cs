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
        await _service.WaitForInit(onError);
        Calendar = await CalendarMonthViewModel.Get(DateTime.Today.MonthOf(), _service.GetAssessmentsByMonth(DateTime.Today.Month, onError));
        Calendar.GetEventsTask = (date) => _service.GetAssessmentsByMonth(date.Month, onError);
        Calendar.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Calendar = await CalendarMonthViewModel.Get(Calendar.Month, _service.GetAssessmentsByMonth(Calendar.Month.Month, OnError.DefaultBehavior(this))); ;
        Calendar.GetEventsTask = (date) => _service.GetAssessmentsByMonth(date.Month, OnError.DefaultBehavior(this));
        Calendar.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }

    [RelayCommand]
    public async Task IncrementMonth()
    {
        await Calendar.IncrementMonth();
    }

    [RelayCommand]
    public async Task DecrementMonth()
    {
        await Calendar.DecrementMonth();
    }
}
