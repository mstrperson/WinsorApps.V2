using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class StudentPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    [ObservableProperty] AllMyStudentsViewModel myStudents;
    [ObservableProperty] StudentViewModel selectedStudent = new();
    [ObservableProperty] bool showStudent;
    [ObservableProperty] bool showStudentSelection;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public StudentPageViewModel(AllMyStudentsViewModel myStudents)
    {
        this.myStudents = myStudents;
        MyStudents.StudentSelected += (_, student) =>
        {
            SelectedStudent = student;
            Busy = true;
            BusyMessage = $"Loading info for {student.UserInfo.DisplayName}";
            ShowStudent = true;
            ShowStudentSelection = false;
            Busy = false;
        };
    }

    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public void ReturnToStudentSelection()
    {
        ShowStudent = false;
        ShowStudentSelection = false;
        SelectedStudent = new();
    }

    [RelayCommand]
    public void ToggleShowStudentSelection() => 
        ShowStudentSelection = !ShowStudentSelection;
}

public partial class AllMyStudentsViewModel :
    ObservableObject,
    IErrorHandling
{
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentViewModel>? StudentSelected;

    public readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();

    [ObservableProperty] ObservableCollection<StudentViewModel> myStudents = [];

    public StudentViewModel? this[string id] => MyStudents.FirstOrDefault(stu => stu.Model.id == id);

    public async Task Initialize(ErrorAction onError)
    {
        await _service.WaitForInit(onError);

        var students = _service.MyStudents;

        MyStudents = [.. 
            students
            .Select(StudentViewModel.Get)
            .OrderBy(student => student.ClassName)
            .ThenBy(student => student.UserInfo.Model.lastName)];
        foreach (var student in MyStudents)
        {
            student.Selected += async (_, _) =>
            {
                await student.UserInfo.GetPhoto();
                StudentSelected?.Invoke(this, student);
            };
            student.UserInfo.GetUniqueDisplayName();
        }
    }
}

public partial class StudentViewModel :
    ObservableObject,
    IModelCarrier<StudentViewModel, StudentRecordShort>,
    IErrorHandling,
    ISelectable<StudentViewModel>,
    IBusyViewModel
{
    private readonly TeacherAssessmentService   _service    = ServiceHelper.GetService<TeacherAssessmentService>();
    private readonly CycleDayCollection         _cycleDays  = ServiceHelper.GetService<CycleDayCollection>();
    private readonly LocalLoggingService        _logging    = ServiceHelper.GetService<LocalLoggingService>();
    private readonly RegistrarService           _registrar  = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty] UserViewModel userInfo = UserViewModel.Empty;
    [ObservableProperty] string advisorName = "";
    [ObservableProperty] string className = "";
    [ObservableProperty] int gradYear;

    [ObservableProperty] CalendarMonthViewModel assessmentCalendar = new();
    [ObservableProperty] bool showCalendar;

    [ObservableProperty] StudentLatePassCollectionViewModel latePassCollection = new(UserViewModel.Empty);
    [ObservableProperty] bool showLatePasses;

    [ObservableProperty] ObservableCollection<SectionViewModel> academicSchedule = [];
    [ObservableProperty] bool showSchedule;

    [ObservableProperty] bool isSelected;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    private async Task<ImmutableArray<AssessmentCalendarEvent>> GetStudentCalendar() =>
        await _service.GetStudentCalendar(
            OnError.DefaultBehavior(this), Model.id, 
            DateOnly.FromDateTime(AssessmentCalendar.Month.AddMonths(-1)), 
            DateOnly.FromDateTime(AssessmentCalendar.Month.AddMonths(2)));

    public StudentRecordShort Model { get; set; }

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentViewModel>? Selected;

    public static StudentViewModel Get(StudentRecordShort model)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        var user = model.GetUserRecord(registrar);
        var uvm = UserViewModel.Get(user);

        var vm = new StudentViewModel()
        {
            UserInfo = uvm,
            AdvisorName = model.advisorName,
            Model = model,
            ClassName = model.className,
            GradYear = model.gradYear,
            LatePassCollection = new(uvm)
        };

        return vm;
    }

    [RelayCommand]
    public async Task ToggleCalendar()
    {
        ShowCalendar = !ShowCalendar;
        if(ShowCalendar)
        {
            Busy = true;
            BusyMessage = "Loading Assessment Calendar";
            await LoadAssessmentCalendar();
            ShowLatePasses = false;
            ShowSchedule = false;
            Busy = false;
        }
    }

    [RelayCommand]
    public async Task ToggleLatePasses()
    {
        ShowLatePasses = !ShowLatePasses;
        if(ShowLatePasses)
        {
            Busy = true;
            BusyMessage = "Loading Late Passes";
            await LatePassCollection.LoadPasses();
            ShowCalendar = false;
            ShowSchedule = false;
            Busy = false;
        }
    }

    [RelayCommand]
    public async Task ToggleSchedule()
    {
        ShowSchedule = !ShowSchedule;
        if(ShowSchedule)
        {
            if (!AcademicSchedule.Any())
            {    
                Busy = true;
                BusyMessage = "Loading Assessment Calendar";
                await LoadSchedule();
                Busy = false;
            }
            ShowCalendar = false;
            ShowLatePasses = false;
        }
    }

    [RelayCommand]
    public async Task LoadSchedule()
    {
        var sections = await _registrar.GetAcademicScheduleFor(UserInfo.Id, OnError.DefaultBehavior(this));
        AcademicSchedule = [..SectionViewModel.GetClonedViewModels(sections)];
    }

    [RelayCommand]
    public async Task IncrementMonth()
    {
        await AssessmentCalendar.IncrementMonth(GetStudentCalendar());
    }

    [RelayCommand]
    public async Task DecrementMonth()
    {
        await AssessmentCalendar.DecrementMonth(GetStudentCalendar());
    }

    public async Task LoadAssessmentCalendar()
    {
        DateTime month = AssessmentCalendar.Month.Year != DateTime.Today.Year ? DateTime.Today.MonthOf() : AssessmentCalendar.Month;
        AssessmentCalendar =
            await CalendarMonthViewModel.Get(month,
            _service.GetStudentCalendar(OnError.DefaultBehavior(this),
             UserInfo.Id, DateOnly.FromDateTime(month), DateOnly.FromDateTime(month.AddMonths(1))));
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}
