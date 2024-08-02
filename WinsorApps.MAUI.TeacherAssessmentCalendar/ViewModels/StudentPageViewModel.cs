using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    [ObservableProperty] string searchText = "";
    [ObservableProperty] ObservableCollection<StudentViewModel> searchResults = [];
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

        MyStudents.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
    }


    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public async Task SearchStudents()
    {
        Busy = true;
        BusyMessage = "Searching Students...";

        SearchResults = [.. MyStudents.MyStudents.Where(student => student.UserInfo.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase))];
        if(SearchResults.Count == 0)
        {
            BusyMessage = "No results.";
            await Task.Delay(2000);
        }

        ShowStudentSelection = SearchResults.Count > 1;

        if (SearchResults.Count == 1)
        {
            SearchResults[0].Select();
        }


        Busy = false;
    }

    [RelayCommand]
    public void ReturnToStudentSelection()
    {
        ShowStudent = false;
        ShowStudentSelection = false;
        SelectedStudent = new();
        SearchText = "";
        SearchResults = [];
    }

    [RelayCommand]
    public void ToggleShowStudentSelection() => 
        ShowStudentSelection = !ShowStudentSelection;
}

public partial class AllMyStudentsViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentViewModel>? StudentSelected;

    public readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();

    [ObservableProperty] ObservableCollection<StudentViewModel> myStudents = [];
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

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
            student.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
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

    [ObservableProperty] ScheduleViewModel academicSchedule = new();
    [ObservableProperty] bool showSchedule;

    [ObservableProperty] SectionAssessmentCalendarViewModel selectedSection = new(new());
    [ObservableProperty] bool showSelectedSection;

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
        vm.LatePassCollection.PropertyChanged += ((IBusyViewModel)vm).BusyChangedCascade;
        vm.AcademicSchedule.PropertyChanged += ((IBusyViewModel)vm).BusyChangedCascade;
        vm.AcademicSchedule.SectionSelected += async (sender, section) =>
        {
            vm.SelectedSection = section;
            await section.LoadAssessments();
            vm.ShowSelectedSection = true;
        };
        return vm;
    }

    [RelayCommand]
    public async Task RequestLatePass()
    {
        if (!ShowSelectedSection || !AcademicSchedule.ShowAssessment)
            return;

        await LatePassCollection.RequestNewPassFor(AcademicSchedule.SelectedAssessment.Model);
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
            Busy = true;
            BusyMessage = "Loading Academic Schedule";
            await LoadSchedule();
            Busy = false;
            ShowCalendar = false;
            ShowLatePasses = false;
        }
    }

    [RelayCommand]
    public async Task LoadSchedule()
    {
        await AcademicSchedule.GetScheduleFor(UserInfo);
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
        AssessmentCalendar.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}
