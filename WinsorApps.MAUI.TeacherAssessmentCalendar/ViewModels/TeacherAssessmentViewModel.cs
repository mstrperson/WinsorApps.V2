using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Global;
using WinsorApps.MAUI.Shared;
using CommunityToolkit.Mvvm.Input;
using AsyncAwaitBestPractices;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class StudentAssessmentRosterEntry :
    ObservableObject,
    ISelectable<StudentAssessmentRosterEntry>
{
    [ObservableProperty] StudentViewModel student;
    [ObservableProperty] bool latePassUsed;
    [ObservableProperty] DateTime latePassTimeStamp;
    [ObservableProperty] bool hasConflicts;
    [ObservableProperty] int conflictCount;
    [ObservableProperty] bool redFlag;
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool passAvailable;

    public event EventHandler<StudentAssessmentRosterEntry>? Selected;
    
    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}


public partial class AssessmentDetailsViewModel : 
    ObservableObject,
    IModelCarrier<AssessmentDetailsViewModel, AssessmentCalendarEvent>,
    IErrorHandling,
    ISelectable<AssessmentDetailsViewModel>,
    IBusyViewModel
{
    public event EventHandler? LoadComplete;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentAssessmentRosterEntry>? StudentSelected;
    public event EventHandler<AssessmentDetailsViewModel>? Selected;

    public Optional<AssessmentCalendarEvent> Model { get; private set; } = Optional<AssessmentCalendarEvent>.None();
    private readonly TeacherAssessmentService _assessmentService = ServiceHelper.GetService<TeacherAssessmentService>();
    private readonly ReadonlyCalendarService _calendarService = ServiceHelper.GetService<ReadonlyCalendarService>();
    private readonly RegistrarService _registrarService = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty]
    string title = "";

    [ObservableProperty]
    string subtitle = "";

    [ObservableProperty]
    string dateLabel = "";

    [ObservableProperty]
    DateTime date;

    [ObservableProperty]
    string listLabel = "";

    [ObservableProperty]
    ObservableCollection<string> classList = [];

    [ObservableProperty]
    ObservableCollection<StudentAssessmentRosterEntry> students = [];

    [ObservableProperty] double studentHeightRequest;
    
    [ObservableProperty]
    ObservableCollection<StudentConflictViewModel> conflicts = [];

    [ObservableProperty] double conflictHeightRequest;

    [ObservableProperty]
    ObservableCollection<LatePassViewModel> passess = [];

    [ObservableProperty] double passessHeightRequest;

    [ObservableProperty] bool showStudents = true;
    [ObservableProperty] bool hasLatePasses;
    [ObservableProperty] bool showLatePasses;
    [ObservableProperty] bool hasConflicts;
    [ObservableProperty] bool showConflicts;
    [ObservableProperty] bool hasRedFlags;
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] DateTime submitted;

    private static readonly double ROW_HEIGHT = 65;
    private static readonly double HEADER_HEIGHT = 80;

    [ObservableProperty] StudentAssessmentRosterEntry selectedStudent = new();

    public AssessmentDetailsViewModel() { }

    [RelayCommand]
    public async Task UseLatePassFor(StudentViewModel student)
    {
        Busy = true;
        BusyMessage = "Submitting Late Pass...";
        await student.LatePassCollection.RequestNewPassFor(Model.Reduce(AssessmentCalendarEvent.Empty));

        LoadAssessmentDetails(true);
        Busy = false;
    }

    [RelayCommand]
    public void ToggleShowStudents()
    {
        ShowStudents = true;
        ShowConflicts = false;
        ShowLatePasses = false;
    }

    [RelayCommand]
    public async Task ToggleShowConflicts()
    {
        ShowStudents = false;
        ShowConflicts = true;
        ShowLatePasses = false;
        await LoadConflicts();
    }

    [RelayCommand]
    public void ToggleShowLatePasses()
    {
        ShowStudents = false;
        ShowConflicts = false;
        ShowLatePasses = true;
    }

    [RelayCommand]
    public async Task WithdrawPassFor(StudentViewModel student)
    {
        Busy = true;
        BusyMessage = "Withdrawing Late Pass...";
        var pass = Passess.FirstOrDefault(pass => pass.Student.Id == student.UserInfo.Id);
        if (pass is not null)
        {
            await _assessmentService.WithdrawLatePassForStudent(student.UserInfo.Id,
                pass.Model.Reduce(AssessmentPassDetail.Empty).assessment.id,
                err => OnError?.Invoke(this, err));
            Passess.Remove(pass);
            LoadAssessmentDetails(true);
        }

        Busy = false;
    }
    
    public void SelectStudent(string studentId)
    {
        var result = Students.FirstOrDefault(student => student.Student.Model.Reduce(UserRecord.Empty).id == studentId);
        if (result is not null)
            SelectedStudent = result;
    }

    public AssessmentDetailsViewModel(AssessmentCalendarEvent @event)
    {
        Model = Optional<AssessmentCalendarEvent>.Some(@event);
        title = @event.summary;
        subtitle = "";
        date = @event.start;
        dateLabel = @event.allDay ? $"{@event.start:dddd, dd MMMM}" : $"{@event.start:dddd, dd MMM h:mm tt}";

        switch (@event.type)
        {
            case "assessment":
                LoadAssessmentDetails();
                break;
            case "note":
                listLabel = "Classes";
                classList = [.. @event.description.Split(';')];
                LoadComplete?.Invoke(this, EventArgs.Empty);
                break;
            case "ap-exam":
                LoadAPExam();
                break;
            default: break;
        }
    }

    private void LoadAPExam()
    {
        var getTask = _calendarService.GetAPExamDetails(Model.Reduce(AssessmentCalendarEvent.Empty).id, OnError.DefaultBehavior(this));
        getTask.WhenCompleted(() =>
        {
            var result = getTask.Result;
            if (result is null)
            {
                Title = "Failed to Load.";
                return;
            }

            var exam = result;

            DateLabel = $"{exam.startDateTime:ddd dd MMM hh:mm tt} - {exam.endDateTime:hh:mm tt}";

            ListLabel = "Students";
        Students =
        [ ..
                UserViewModel.GetClonedViewModels(
                    _registrarService.StudentList
                    .Where(student => exam.studentIds.Contains(student.id)))
                    .Select(student => new StudentAssessmentRosterEntry()
                    {
                        Student = StudentViewModel.Get(student)
                    })
        ];

            foreach (var student in Students)
                student.Selected += (_, selected) => StudentSelected?.Invoke(this, selected);

            SetSizeRequests();
            LoadComplete?.Invoke(this, EventArgs.Empty);
        });
    }

    private void SetSizeRequests()
    {
        StudentHeightRequest = HEADER_HEIGHT + Students.Count * ROW_HEIGHT;
        ConflictHeightRequest = HEADER_HEIGHT + Conflicts.Count * ROW_HEIGHT;
        PassessHeightRequest = HEADER_HEIGHT + Passess.Count * ROW_HEIGHT;
    }

    private void LoadAssessmentDetails(bool refreshCache = false)
    {
        Busy = true;
        BusyMessage = "Loading Assessment Details.";
        var getTask = _assessmentService.GetAssessmentDetails(Model.Reduce(AssessmentCalendarEvent.Empty).id, OnError.DefaultBehavior(this), refreshCache);
        getTask.WhenCompleted(() =>
        {
            var result = getTask.Result;
            if (result is null)
            {
                Title = "Unable to Load Assessment";
                return;
            }

            var details = result;
            Submitted = details.submitted;
            Title = string.IsNullOrEmpty(Model.Reduce(AssessmentCalendarEvent.Empty).description) ? 
                Model.Reduce(AssessmentCalendarEvent.Empty).summary : 
                Model.Reduce(AssessmentCalendarEvent.Empty).description;
            Subtitle = $"{details.section.displayName} [{details.section.teachers
                    .Select(t => $"{t}")
                    .Aggregate((a, b) => $"{a}, {b}")}]";
            ListLabel = "Students";
            Students =
            [..
                UserViewModel.GetClonedViewModels(
                    _registrarService.StudentList
                    .Where(student => details.section.students.Any(stu => stu.id == student.id)))
                    .Select(student => new StudentAssessmentRosterEntry()
                    {
                        Student = StudentViewModel.Get(student),
                        LatePassUsed = details.studentsUsingPasses.Any(pass => pass.student.id == student.Id),
                        LatePassTimeStamp = details.studentsUsingPasses.FirstOrDefault(pass => pass.student.id == student.Id)?.timeStamp ?? default,
                        HasConflicts = details.studentConflicts.Any(conflict => conflict.student.id == student.Id),
                        ConflictCount = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id) ?.count ?? 0,
                        RedFlag = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id) ?.redFlag ?? default,
                        PassAvailable = details.studentsWithPassAvailable.Any(stu => stu.id == student.Id)
                    })
            ];

            foreach (var student in Students)
                student.Selected += (_, selected) => StudentSelected?.Invoke(this, selected);

            Passess = LatePassViewModel.GetPasses(details);
            HasLatePasses = Passess.Any();

            Conflicts = [.. details.studentConflicts.Select(StudentConflictViewModel.Get)];

            //LoadConflicts().SafeFireAndForget(e => e.LogException());

            HasConflicts = Conflicts.Any();
            HasRedFlags = Conflicts.Any(conflict => conflict.RedFlag);

            SetSizeRequests();
            LoadComplete?.Invoke(this, EventArgs.Empty);
            Busy = false;
        });
    }
    private async Task LoadAssessmentDetailsAsync(bool refreshCache = false)
    {
        Busy = true;
        BusyMessage = "Loading Assessment Details.";
        var result = await _assessmentService.GetAssessmentDetails(Model.Reduce(AssessmentCalendarEvent.Empty).id, OnError.DefaultBehavior(this), refreshCache);

        if (result is null)
        {
            Title = "Unable to Load Assessment";
            return;
        }

        var details = result;
        Submitted = details.submitted;
        Title = string.IsNullOrEmpty(Model.Reduce(AssessmentCalendarEvent.Empty).description) ?
            Model.Reduce(AssessmentCalendarEvent.Empty).summary :
            Model.Reduce(AssessmentCalendarEvent.Empty).description;
        Subtitle = $"{details.section.displayName} [{details.section.teachers
                .Select(t => $"{t}")
                .Aggregate((a, b) => $"{a}, {b}")}]";
        ListLabel = "Students";
        Students =
        [..
                UserViewModel.GetClonedViewModels(
                    _registrarService.StudentList
                    .Where(student => details.section.students.Any(stu => stu.id == student.id)))
                    .Select(student => new StudentAssessmentRosterEntry()
                    {
                        Student = StudentViewModel.Get(student),
                        LatePassUsed = details.studentsUsingPasses.Any(pass => pass.student.id == student.Id),
                        LatePassTimeStamp = details.studentsUsingPasses.FirstOrDefault(pass => pass.student.id == student.Id)?.timeStamp ?? default,
                        HasConflicts = details.studentConflicts.Any(conflict => conflict.student.id == student.Id),
                        ConflictCount = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id) ?.count ?? 0,
                        RedFlag = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id) ?.redFlag ?? default,
                        PassAvailable = details.studentsWithPassAvailable.Any(stu => stu.id == student.Id)
                    })
        ];

        foreach (var student in Students)
            student.Selected += (_, selected) => StudentSelected?.Invoke(this, selected);

        Passess = LatePassViewModel.GetPasses(details);
        HasLatePasses = Passess.Any();

        Conflicts = [.. details.studentConflicts.Select(StudentConflictViewModel.Get)];

        //LoadConflicts().SafeFireAndForget(e => e.LogException());

        HasConflicts = Conflicts.Any();
        HasRedFlags = Conflicts.Any(conflict => conflict.RedFlag);

        SetSizeRequests();
        LoadComplete?.Invoke(this, EventArgs.Empty);
        Busy = false;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        if(Model.Reduce(AssessmentCalendarEvent.Empty).type == AssessmentType.Assessment)
        {
            await LoadAssessmentDetailsAsync(true);
        }
    }


    [RelayCommand]
    public async Task LoadConflicts()
    {
        Busy = true;
        BusyMessage = "Checking for Conflicts";
        foreach (var conflict in Conflicts)
            await conflict.LoadConflictingAssessments();
        Busy = false;
    }

    public static AssessmentDetailsViewModel Get(AssessmentEntryRecord details)
    {
        var registrarService = ServiceHelper.GetService<RegistrarService>();
        var service = ServiceHelper.GetService<TeacherAssessmentService>();
        
        var vm = new AssessmentDetailsViewModel();

        var group = service.MyAssessments.FirstOrDefault(grp => grp.id == details.groupId);

        vm.Model = group is not null ?
            Optional<AssessmentCalendarEvent>.Some(details.ToCalendarEvent(group)) :
            Optional<AssessmentCalendarEvent>.None();

        vm.DateLabel = $"{details.assessmentDateTime:dddd dd MMMM hh:mm tt}";
        vm.Date = details.assessmentDateTime;
        vm.Title = (string.IsNullOrEmpty(group?.note) ? group?.course : group?.note) ?? "";
        vm.Subtitle = $"{details.section.displayName} [{details.section.teachers
                .Select(t => $"{t}")
                .Aggregate((a, b) => $"{a}, {b}")}]";
        vm.ListLabel = "Students";
        vm.Students =
        [..
                UserViewModel.GetClonedViewModels(
                    registrarService.StudentList
                    .Where(student => details.section.students.Any(stu => stu.id == student.id)))
                    .Select(student => new StudentAssessmentRosterEntry()
                    {
                        Student = StudentViewModel.Get(student),
                        LatePassUsed = details.studentsUsingPasses.Any(pass => pass.student.id == student.Id),
                        LatePassTimeStamp = details.studentsUsingPasses.FirstOrDefault(pass => pass.student.id == student.Id)?.timeStamp ?? default,
                        HasConflicts = details.studentConflicts.Any(conflict => conflict.student.id == student.Id),
                        ConflictCount = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id) ?.count ?? 0,
                        RedFlag = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id) ?.redFlag ?? false,
                        PassAvailable = details.studentsWithPassAvailable.Any(stu => stu.id == student.Id)
                    })
        ];

        vm.Passess = LatePassViewModel.GetPasses(details);
        vm.HasLatePasses = vm.Passess.Any();

        vm.Conflicts = [.. details.studentConflicts.Select(StudentConflictViewModel.Get)];

        //vm.LoadConflicts().SafeFireAndForget(e => e.LogException());

        vm.HasConflicts = vm.Conflicts.Any();
        vm.HasRedFlags = vm.Conflicts.Any(conflict => conflict.RedFlag);

        vm.SetSizeRequests();
        return vm;
    }

    public static AssessmentDetailsViewModel Get(AssessmentCalendarEvent model) => new(model);

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}


public partial class StudentConflictViewModel : 
    ObservableObject,
    IModelCarrier<StudentConflictViewModel, StudentConflictCount>, 
    IErrorHandling
{
    private readonly RegistrarService _regsitrar = ServiceHelper.GetService<RegistrarService>();
    private readonly TeacherAssessmentService _calendar = ServiceHelper.GetService<TeacherAssessmentService>();

    [ObservableProperty] UserViewModel student = UserViewModel.Empty;
    [ObservableProperty] int conflictCount;
    [ObservableProperty] bool latePassUsed;
    [ObservableProperty] bool redFlag;
    [ObservableProperty] ObservableCollection<AssessmentDetailsViewModel> conflictingAssessments = [];
    [ObservableProperty] string conflictList = "";

    public event EventHandler<ErrorRecord>? OnError;

    public Optional<StudentConflictCount> Model { get; private set; } = Optional<StudentConflictCount>.None();

    public StudentConflictViewModel(StudentConflictCount conflict)
    {
        Model = Optional<StudentConflictCount>.Some(conflict);
        Student = UserViewModel.Get(conflict.student.GetUserRecord(_regsitrar));
        ConflictCount = conflict.count;
        LatePassUsed = conflict.latePass;
        RedFlag = conflict.redFlag;
    }

    [RelayCommand]
    public async Task LoadConflictingAssessments()
    {
        var assessmentIds = Model.Reduce(StudentConflictCount.Empty).assessmentIds;

        ConflictingAssessments = [];

        foreach(var id in assessmentIds)
        {
            var details = await _calendar.GetAssessmentDetails(id, OnError.DefaultBehavior(this));
            if (details is null)
                continue;

            ConflictingAssessments.Add(AssessmentDetailsViewModel.Get(details));
        }

        ConflictList = ConflictingAssessments.Select(details => $"{details.Title} - {details.Subtitle}").DelimeteredList(Environment.NewLine);
    }

    public static StudentConflictViewModel Get(StudentConflictCount model) => new(model);
}
