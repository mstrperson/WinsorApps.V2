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
    IErrorHandling,
    IBusyViewModel
{
    private readonly StudentAssessmentService _service;
    private readonly LocalLoggingService _logging;
    private readonly CycleDayCollection _cycleDays;

    [ObservableProperty] StudentWeekViewModel calendar =new();

    // I forgot why we wanted this? but here it is? lol
    [ObservableProperty] StudentAssessmentViewModel selectedAssessment = new(new());
    [ObservableProperty] bool showSelectedAssessment;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<StudentAssessmentViewModel>? EventSelected;

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
        await UpdateCalendar(nextWeek);
    }

    private async Task UpdateCalendar(DateTime nextWeek)
    {
        _ = await _cycleDays.GetCycleDays(DateOnly.FromDateTime(nextWeek), DateOnly.FromDateTime(nextWeek).AddDays(7), OnError.DefaultBehavior(this));

        _ = await _service.GetMyCalendarInRange(OnError.DefaultBehavior(this), nextWeek, nextWeek.AddDays(7));

        Calendar = CalendarWeekViewModel.Get(nextWeek, _service.MyCalendar);

        Calendar.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        Calendar.Week.EventSelected += (_, e) => EventSelected?.Invoke(this, e);
        Calendar.AssessmentSelected += (_, assessment) =>
        {
            ShowSelectedAssessment = assessment.IsSelected;
            SelectedAssessment = assessment;
            // Reset all other assessment selections.
            foreach (var asmt in Calendar.Days.SelectMany(day => day.Assessments))
            {
                asmt.IsSelected = assessment.Event.Model.Reduce(AssessmentCalendarEvent.Empty).id == asmt.Event.Model.Reduce(AssessmentCalendarEvent.Empty).id && assessment.IsSelected;
            }
        };
    }

    [RelayCommand]
    public async Task DecrementWeek()
    {
        var nextWeek = Calendar.Week.Monday.AddDays(-7);
        await UpdateCalendar(nextWeek);
    }

    [RelayCommand]
    public async Task Refresh()
    {
        await UpdateCalendar(Calendar.Week.Monday);
    }

    public async Task Initialize(ErrorAction onError)
    {
        await _service.WaitForInit(onError);
        Calendar.Week.Monday = DateTime.Today.MondayOf();
        await Refresh();
    }
}

public partial class StudentDayViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    [ObservableProperty] private ObservableCollection<StudentAssessmentViewModel> assessments = [];
    [ObservableProperty] private CalendarDayViewModel day = new();

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentAssessmentViewModel>? AssessmentSelected;

    public static implicit operator StudentDayViewModel(CalendarDayViewModel day)
    {
        var vm = new StudentDayViewModel()
        {
            Day = day,
            Assessments = [.. day.Events.Where(e => e.Model.Reduce(AssessmentCalendarEvent.Empty).type == AssessmentType.Assessment)]
        };

        foreach(var assessment in vm.Assessments)
        {
            assessment.OnError += (sender, e) => vm.OnError?.Invoke(sender, e);
            assessment.Selected += (_, _) => vm.AssessmentSelected?.Invoke(vm, assessment);
            assessment.PropertyChanged += ((IBusyViewModel)vm).BusyChangedCascade;
        }

        return vm;
    }
}
public partial class StudentWeekViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    [ObservableProperty] private ObservableCollection<StudentDayViewModel> days = [];
    [ObservableProperty] private CalendarWeekViewModel week = new();

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentAssessmentViewModel>? AssessmentSelected;


    public static implicit operator StudentWeekViewModel(CalendarWeekViewModel week)
    {
        var vm = new StudentWeekViewModel()
        {
            Week = week,
            Days = [.. week.Days]
        };

        foreach(var day in vm.Days)
        {
            day.OnError += (sender, e) => vm.OnError?.Invoke(sender, e);
            day.AssessmentSelected += (_, assessment) => vm.AssessmentSelected?.Invoke(vm, assessment);
            day.PropertyChanged += ((IBusyViewModel)vm).BusyChangedCascade;
        }

        return vm;
    }
        
}