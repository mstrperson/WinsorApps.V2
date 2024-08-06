using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;

public partial class WeeklyViewModel :
    ObservableObject,
    IErrorHandling
{
    private readonly StudentAssessmentService _service;
    private readonly LocalLoggingService _logging;
    private readonly CycleDayCollection _cycleDays;

    [ObservableProperty] StudentWeekViewModel calendar =new();

    public event EventHandler<AssessmentCalendarEventViewModel>? EventSelected;

    public WeeklyViewModel(StudentAssessmentService service, LocalLoggingService logging, CycleDayCollection cycleDays)
    {
        _service = service;
        _logging = logging;
        _cycleDays = cycleDays;

    }

    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public async Task IncrementWeek()
    {
        var nextWeek = Calendar.Week.Monday.AddDays(7);

        _ = await _cycleDays.GetCycleDays(DateOnly.FromDateTime(nextWeek), DateOnly.FromDateTime(nextWeek).AddDays(1), OnError.DefaultBehavior(this));

        _ = await _service.GetMyCalendarInRange(OnError.DefaultBehavior(this), nextWeek, nextWeek.AddDays(1));

        Calendar = CalendarWeekViewModel.Get(nextWeek, _service.MyCalendar);
        Calendar.Week.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }

    [RelayCommand]
    public async Task DecrementWeek()
    {
        var nextWeek = Calendar.Week.Monday.AddDays(-7);

        _ = await _cycleDays.GetCycleDays(DateOnly.FromDateTime(nextWeek),DateOnly.FromDateTime(nextWeek).AddDays(7), OnError.DefaultBehavior(this));

        _ = await _service.GetMyCalendarInRange(OnError.DefaultBehavior(this), nextWeek, nextWeek.AddDays(7));

        Calendar = CalendarWeekViewModel.Get(nextWeek, _service.MyCalendar);
        Calendar.Week.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }

    [RelayCommand]
    public async Task Refresh()
    {

        _ = await _service.GetMyCalendarInRange(OnError.DefaultBehavior(this), Calendar.Week.Monday, Calendar.Week.Monday.AddDays(7));

        Calendar = CalendarWeekViewModel.Get(Calendar.Week.Monday, _service.MyCalendar);
        Calendar.Week.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
    }

    public async Task Initialize(ErrorAction onError)
    {
        await _service.WaitForInit(onError);
        Calendar.Week.Monday = DateTime.Today.MondayOf();
        await Refresh();
    }
}

public partial class StudentDayViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<StudentAssessmentViewModel> assessments = [];
    [ObservableProperty] private CalendarDayViewModel day = new();

    public static implicit operator StudentDayViewModel(CalendarDayViewModel day)
        => new() {Day = day, Assessments = [..day.Events.Where(e => e.Model.type == AssessmentType.Assessment)]};
}
public partial class StudentWeekViewModel: ObservableObject
{
    [ObservableProperty] private ObservableCollection<StudentDayViewModel> days = [];
    [ObservableProperty] private CalendarWeekViewModel week = new();

    public static implicit operator StudentWeekViewModel(CalendarWeekViewModel week)
        => new() {Week = week, Days = [..week.Days]};
}