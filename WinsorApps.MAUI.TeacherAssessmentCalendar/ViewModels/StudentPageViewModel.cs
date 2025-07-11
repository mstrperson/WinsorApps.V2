﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    [ObservableProperty] private AllMyStudentsViewModel myStudents;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private ObservableCollection<StudentViewModel> searchResults = [];
    [ObservableProperty] private double searchResultsHeight;
    private readonly double SEARCH_ROW_HEIGHT = 35;
    private readonly double SEARCH_HEADER_HEIGHT = 45;

    [ObservableProperty] private StudentViewModel selectedStudent = new();
    [ObservableProperty] private bool showStudent;
    [ObservableProperty] private bool showStudentSelection;

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    public event EventHandler<AssessmentDetailsViewModel>? AssessmentSelected;

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

        foreach (var student in MyStudents.MyStudents)
            student.AssessmentSelected += (sender, evt) => AssessmentSelected?.Invoke(sender, evt);

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

        SearchResultsHeight = SEARCH_ROW_HEIGHT * SearchResults.Count + SEARCH_HEADER_HEIGHT;

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

    [ObservableProperty] private ObservableCollection<StudentViewModel> myStudents = [];
    [ObservableProperty] private int studentCount;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    public StudentViewModel? this[string id] => MyStudents.FirstOrDefault(stu => stu.Model.Reduce(UserRecord.Empty).id == id);

    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();
    
    public async Task Initialize(ErrorAction onError)
    {
        await _service.WaitForInit(onError);

        await _registrar.WaitForUniqueNames();
        
        var students = _registrar.StudentList.Select(u => (StudentRecordShort)u);

        MyStudents = [.. 
            students
            .Select(StudentViewModel.Get)
            .OrderBy(student => student.ClassName)
            .ThenBy(student => student.UserInfo.Model.Reduce(UserRecord.Empty).lastName)];
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

        StudentCount = MyStudents.Count;
    }
}

public partial class StudentViewModel :
    ObservableObject,
    IModelCarrier<StudentViewModel, StudentRecordShort>,
    IErrorHandling,
    ISelectable<StudentViewModel>,
    IBusyViewModel
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();
    private readonly CycleDayCollection _cycleDays = ServiceHelper.GetService<CycleDayCollection>();
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();
    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();


    [ObservableProperty] private UserViewModel userInfo = UserViewModel.Empty;
    [ObservableProperty] private string advisorName = "";
    [ObservableProperty] private string className = "";
    [ObservableProperty] private int gradYear;

    [ObservableProperty] private CalendarMonthViewModel assessmentCalendar = new();
    [ObservableProperty] private bool showCalendar;

    [ObservableProperty] private StudentLatePassCollectionViewModel latePassCollection = new(UserViewModel.Empty);
    [ObservableProperty] private bool showLatePasses;

    [ObservableProperty] private ScheduleViewModel academicSchedule = new();
    [ObservableProperty] private bool showSchedule;

    [ObservableProperty] private SectionAssessmentCalendarViewModel selectedSection;
    [ObservableProperty] private bool showSelectedSection;

    [ObservableProperty] private ObservableCollection<LateWorkViewModel> lateWork = [];
    [ObservableProperty] private bool showLateWork;
    [ObservableProperty] private bool includeResolvedLateWork;

    [ObservableProperty] private bool isSelected;

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    public static implicit operator UserViewModel(StudentViewModel student) => student.UserInfo;

    private async Task<List<AssessmentCalendarEvent>> GetStudentCalendar() =>
        await _service.GetStudentCalendar(
            OnError.DefaultBehavior(this), Model.Reduce(UserRecord.Empty).id,
            DateOnly.FromDateTime(AssessmentCalendar.Month.AddMonths(-1)),
            DateOnly.FromDateTime(AssessmentCalendar.Month.AddMonths(2)));

    public Optional<StudentRecordShort> Model { get; set; } = Optional<StudentRecordShort>.None();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentViewModel>? Selected;

    public event EventHandler<AssessmentDetailsViewModel>? AssessmentSelected;

    public static StudentViewModel Get(UserViewModel user)
    {
        var student = (StudentRecordShort)user.Model.Reduce(UserRecord.Empty);
        return Get(student);
    }

    public static StudentViewModel Get(StudentRecordShort model)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        var user = model.GetUserRecord(registrar);
        var uvm = UserViewModel.Get(user);
        if (string.IsNullOrWhiteSpace(uvm.DisplayName))
        {
            Debug.WriteLine("wtf");
        }
        uvm.GetUniqueDisplayName();


        var vm = new StudentViewModel()
        {
            UserInfo = uvm,
            AdvisorName = model.advisorName,
            Model = Optional<StudentRecordShort>.Some(model),
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
            foreach (var assessment in vm.SelectedSection.Assessments)
                assessment.Selected += (_, _) => vm.AssessmentSelected?.Invoke(vm, assessment);
            vm.ShowSelectedSection = true;
        };
        return vm;
    }

    [RelayCommand]
    public async Task ToggleShowLateWork()
    {
        ShowLateWork = !ShowLateWork;
        if (ShowLateWork)
        {
            await LoadLateWork();
            ShowCalendar = false;
            ShowLatePasses = false;
            ShowSchedule = false;
        }
    }

    [RelayCommand]
    public async Task ToggleShowResolved()
    {
        IncludeResolvedLateWork = !IncludeResolvedLateWork;
        await LoadLateWork();
    }

    [RelayCommand]
    public async Task LoadLateWork()
    {
        Busy = true;
        BusyMessage = "Loading Late Work";
        var result = await _service.GetStudentLateWork(OnError.DefaultBehavior(this), UserInfo.Id, IncludeResolvedLateWork);
        
        if(result is not null)
        {
            LateWork = [..result.Select(LateWorkViewModel.Get)];
        }

        Busy = false;
    }

    [RelayCommand]
    public async Task RequestLatePass()
    {
        if (!ShowSelectedSection || !AcademicSchedule.ShowAssessment)
            return;

        await LatePassCollection.RequestNewPassFor(AcademicSchedule.SelectedAssessment.Model.Reduce(AssessmentCalendarEvent.Empty));
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
            ShowLateWork = false;
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
            ShowLateWork = false;
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
            ShowLateWork = false;
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
        var result = await
            _service.GetStudentCalendar(OnError.DefaultBehavior(this),
             UserInfo.Id, DateOnly.FromDateTime(month), DateOnly.FromDateTime(month.AddMonths(1)));
        AssessmentCalendar =
            CalendarMonthViewModel.Get(month, result); 
        AssessmentCalendar.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        AssessmentCalendar.EventSelected += (sender, e) => AssessmentSelected?.Invoke(sender, AssessmentDetailsViewModel.Get(e.Model.Reduce(AssessmentCalendarEvent.Empty)));
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}
