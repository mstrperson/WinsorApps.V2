using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class APExamDetailViewModel :
    ObservableObject,
    IErrorHandling,
    ISelectable<APExamDetailViewModel>,
    IModelCarrier<APExamDetailViewModel, APExamDetail>,
    IBusyViewModel
{

    private readonly AssessmentCalendarRestrictedService _service = ServiceHelper.GetService<AssessmentCalendarRestrictedService>();

    [ObservableProperty] bool isSelected;
    public Optional<APExamDetail> Model { get; private set; } = Optional<APExamDetail>.None();

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Loading...";

    [ObservableProperty] string id = "";
    [ObservableProperty] string courseName = "";
    [ObservableProperty] DateTime date = DateTime.Now;
    [ObservableProperty] TimeSpan startTime = TimeSpan.Zero;
    [ObservableProperty] TimeSpan endTime = TimeSpan.Zero;
    [ObservableProperty] ObservableCollection<SectionViewModel> sections = [];
    [ObservableProperty] ObservableCollection<StudentViewModel> students = [];

    [ObservableProperty] APExamConflictReportViewModel conflicts = new();

    [ObservableProperty] UserSearchViewModel studentSearch = new UserSearchViewModel();


    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<APExamDetailViewModel>? Selected;
    public event EventHandler? Saved;
    public event EventHandler<APExamDetailViewModel>? Deleted;

    public APExamDetailViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        studentSearch.OnSingleResult += StudentSearchOnOnSingleResult;
        studentSearch.OnError += OnError;
        studentSearch.SetAvailableUsers(registrar.StudentList);
    }

    public async Task LoadFromEventData(AssessmentCalendarEvent evt)
    {
        if (evt.type != AssessmentType.ApExam)
            return;

        var model = await _service.GetAPExam(evt.id, OnError.DefaultBehavior(this));
        if (model is null)
            return;

        Busy = true;
        BusyMessage = "Fetching Exam Info";

        var registrar = ServiceHelper.GetService<RegistrarService>();

        Id = model.id;
        CourseName = model.courseName;
        Date = model.startDateTime.Date;
        StartTime = TimeOnly.FromDateTime(model.startDateTime).ToTimeSpan();
        EndTime = TimeOnly.FromDateTime(model.endDateTime).ToTimeSpan();
        Sections = [.. model.sectionIds
                            .Select(id => registrar.SectionDetailCache.ContainsKey(id) ? registrar.SectionDetailCache[id] : null)
                            .Where(sec => sec is not null)
                            .Select(sec => SectionViewModel.Get(sec!))];
        Students = [.. model.studentIds
                            .Select(id => registrar.StudentList.FirstOrDefault(stu => stu.id == id))
                            .Where(stu => stu is not null)
                            .Select(stu => StudentViewModel.Get(stu!))];
        Model = Optional<APExamDetail>.Some(model);
        
        foreach (var student in Students)
        {
            student.OnError += OnError;
            student.Selected += (_, _) => Students.Remove(student);
        }

        await LoadConflicts();

        Busy = false;
    }

    private void StudentSearchOnOnSingleResult(object? sender, UserViewModel e)
    {
        if (Students.Any(stu => stu.UserInfo.Id == e.Id))
            return;
        var studentViewModel = StudentViewModel.Get(e);
        studentViewModel.OnError += OnError;
        studentViewModel.Selected += (_, _) => Students.Remove(studentViewModel);
        Students.Add(studentViewModel);
        StudentSearch.ClearSelection();
    }

    public static APExamDetailViewModel Get(APExamDetail model)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();

        var vm = new APExamDetailViewModel()
        {
            Id = model.id,
            CourseName = model.courseName,
            Date = model.startDateTime.Date,
            StartTime = TimeOnly.FromDateTime(model.startDateTime).ToTimeSpan(),
            EndTime = TimeOnly.FromDateTime(model.endDateTime).ToTimeSpan(),
            Sections = [.. model.sectionIds
                                .Select(id => registrar.SectionDetailCache.ContainsKey(id) ? registrar.SectionDetailCache[id] : null)
                                .Where(sec => sec is not null)
                                .Select(sec => SectionViewModel.Get(sec!))],
            Students = [.. model.studentIds
                                .Select(id => registrar.StudentList.FirstOrDefault(stu => stu.id == id))
                                .Where(stu => stu is not null)
                                .Select(stu => StudentViewModel.Get(stu!))],
            Model = Optional<APExamDetail>.Some(model)
        };

        foreach (var student in vm.Students)
        {
            student.OnError += vm.OnError;
            student.Selected += (_, _) => vm.Students.Remove(student);
        }

        return vm;
    }

    [RelayCommand]
    public async Task LoadConflicts()
    {
        Busy = true;
        BusyMessage = "Loading conflicts";
        var report = await _service.GetAPConflicts(Id, OnError.DefaultBehavior(this));
        if (report is not null)
            Conflicts = new APExamConflictReportViewModel(report);
        Busy = false;
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }

    [RelayCommand]
    public async Task SaveChanges()
    {
        Busy = true;
        BusyMessage = "Saving Changes";
        if (string.IsNullOrEmpty(Id))
        {
            var result = await _service.CreateAPExam(
                GetCreateAPExam(),
                OnError.DefaultBehavior(this));
            if (result is not null)
            {
                Model = Optional<APExamDetail>.Some(result);
                Id = result.id;
            }
        }
        else
        {
            var result2 = await _service.UpdateAPExam(
                Id,
                GetCreateAPExam(),
                OnError.DefaultBehavior(this));
            if (result2 is not null)
                Model = Optional<APExamDetail>.Some(result2);
        }
        Busy = false;
        Saved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task Delete()
    {
        if (!string.IsNullOrEmpty(Id))
            await _service.DeleteAPExam(Id, OnError.DefaultBehavior(this));
        Deleted?.Invoke(this, this);
    }

    private CreateAPExam GetCreateAPExam() => 
        new (
            CourseName,
            Date.Date.Add(StartTime),
            Date.Date.Add(EndTime),
            [.. Sections.Select(sec => sec.Id)],
            [.. Students.Select(stu => stu.UserInfo.Id)]);
}

public partial class APExamPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly AssessmentCalendarRestrictedService _service = ServiceHelper.GetService<AssessmentCalendarRestrictedService>();
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Loading...";
    [ObservableProperty] ObservableCollection<APExamDetailViewModel> exams = [];

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<APExamDetailViewModel>? CreateAPRequested;
    public event EventHandler<APExamDetailViewModel>? OnSelected;
    public event EventHandler? PopRequested;

    public APExamPageViewModel()
    {
        Refresh().SafeFireAndForget(e => e.LogException());
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Loading Exams";
        var result = await _service.GetAPExams(OnError.DefaultBehavior(this));
        if (result is not null)
        {
            Exams = [.. result
                .OrderBy(e => e.startDateTime)
                .Select(APExamDetailViewModel.Get)];
            foreach (var exam in Exams)
            {
                exam.Selected += (_, exam) => OnSelected?.Invoke(this, exam);
                exam.OnError += (sender, err) => OnError?.Invoke(sender, err);
                exam.Deleted += (_, _) =>
                {
                    Exams.Remove(exam);
                    PopRequested?.Invoke(this, EventArgs.Empty);
                };
                exam.Saved += (_, _) => PopRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        Busy = false;
    }

    [RelayCommand]
    public void CreateNew()
    {
        var vm = new APExamDetailViewModel();
        vm.Deleted += (_, _) =>
        {
            Exams.Remove(vm);
            PopRequested?.Invoke(this, EventArgs.Empty);
        };
        vm.Saved += (_, _) => PopRequested?.Invoke(this, EventArgs.Empty);
        vm.OnError += (sender, err) => OnError?.Invoke(sender, err);
        Exams.Add(vm);
        CreateAPRequested?.Invoke(this, vm);
    }
        
}

public partial class APExamConflict :
    ObservableObject
{
    private static readonly double HEADER_HEIGHT = 75;
    private static readonly double STUDENT_HEIGHT = 50;
    [ObservableProperty] SectionViewModel section = SectionViewModel.Empty;
    [ObservableProperty] ObservableCollection<UserViewModel> students = [];
    [ObservableProperty] bool showStudents;
    [ObservableProperty] double heightRequest = HEADER_HEIGHT;
    public APExamConflict(APExamSectionConflict model)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        var studentIds = model.studentsInExam
            .Select(stu => stu.id)
            .ToHashSet();
        Section = SectionViewModel.Get(model.section);
        Students = 
            [.. 
                registrar.StudentList
                    .Where(u => studentIds.Contains(u.id))
                    .Select(UserViewModel.Get)
            ];
    }

    [RelayCommand]
    public void ToggleShowStudents()
    {
        ShowStudents = !ShowStudents;
        HeightRequest =
            ShowStudents
                ? HEADER_HEIGHT + (Students.Count * STUDENT_HEIGHT)
                : HEADER_HEIGHT;
    }
}

public partial class APExamConflictReportViewModel :
    ObservableObject
{
    private static readonly double HEADER_HEIGHT = 50;
    [ObservableProperty] ObservableCollection<APExamConflict> conflicts = [];
    [ObservableProperty] double heightRequest;
    public APExamConflictReportViewModel()
    {
        conflicts = [];
    }

    public APExamConflictReportViewModel(APExamSectionConflictReport model)
    {
        conflicts =
            [..
                model.conflicts
                    .OrderBy(conflict => conflict.section.block)
                    .Select(conflict => new APExamConflict(conflict))
            ];

        foreach (var conflict in conflicts)
            conflict.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(APExamConflict.HeightRequest))
                    HeightRequest = HEADER_HEIGHT + conflicts.Sum(c => c.HeightRequest);
            };

        HeightRequest = HEADER_HEIGHT + conflicts.Sum(c => c.HeightRequest);
    }
}
