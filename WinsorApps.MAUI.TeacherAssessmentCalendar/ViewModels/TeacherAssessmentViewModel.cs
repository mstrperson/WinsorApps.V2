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

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class StudentAssessmentRosterEntry :
    ObservableObject,
    ISelectable<StudentAssessmentRosterEntry>
{
    [ObservableProperty] UserViewModel student = UserViewModel.Empty;
    [ObservableProperty] bool latePassUsed;
    [ObservableProperty] DateTime latePassTimeStamp;
    [ObservableProperty] bool hasConflicts;
    [ObservableProperty] int conflictCount;
    [ObservableProperty] bool redFlag;
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool passAvailable;

    public event EventHandler<StudentAssessmentRosterEntry>? Selected;

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
    ISelectable<AssessmentDetailsViewModel>
{
    public event EventHandler? LoadComplete;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentAssessmentRosterEntry>? StudentSelected;
    public event EventHandler<AssessmentDetailsViewModel>? Selected;

    public OptionalStruct<AssessmentCalendarEvent> Model { get; private set; } = OptionalStruct<AssessmentCalendarEvent>.None();
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
    
    [ObservableProperty]
    ObservableCollection<StudentConflictViewModel> conflicts = [];

    [ObservableProperty]
    ObservableCollection<LatePassViewModel> passess = [];

    [ObservableProperty] bool hasLatePasses;
    [ObservableProperty] bool hasConflicts;
    [ObservableProperty] bool hasRedFlags;
    [ObservableProperty] bool isSelected;

    [ObservableProperty] StudentAssessmentRosterEntry selectedStudent = new();

    public AssessmentDetailsViewModel() { }

    public void SelectStudent(string studentId)
    {
        var result = Students.FirstOrDefault(student => student.Student.Model.Reduce(UserRecord.Empty).id == studentId);
        if (result is not null)
            SelectedStudent = result;
    }

    public AssessmentDetailsViewModel(AssessmentCalendarEvent @event)
    {
        Model = OptionalStruct<AssessmentCalendarEvent>.Some(@event);
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
            if (!result.HasValue)
            {
                Title = "Failed to Load.";
                return;
            }

            var exam = result.Value;

            DateLabel = $"{exam.startDateTime:ddd dd MMM hh:mm tt} - {exam.endDateTime:hh:mm tt}";

            ListLabel = "Students";
            Students =
            [ ..
                UserViewModel.GetClonedViewModels(
                    _registrarService.StudentList
                    .Where(student => exam.studentIds.Contains(student.id)))
                    .Select(student => new StudentAssessmentRosterEntry()
                    {
                        Student = student
                    })
            ];

            foreach (var student in Students)
                student.Selected += (_, selected) => StudentSelected?.Invoke(this, selected);

            LoadComplete?.Invoke(this, EventArgs.Empty);
        });
    }

    private void LoadAssessmentDetails()
    {
        var getTask = _assessmentService.GetAssessmentDetails(Model.Reduce(AssessmentCalendarEvent.Empty).id, OnError.DefaultBehavior(this));
        getTask.WhenCompleted(() =>
        {
            var result = getTask.Result;
            if (!result.HasValue)
            {
                Title = "Unable to Load Assessment";
                return;
            }

            var details = result.Value;
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
                        Student = student,
                        LatePassUsed = details.studentsUsingPasses.Any(pass => pass.student.id == student.Id),
                        LatePassTimeStamp = details.studentsUsingPasses.FirstOrDefault(pass => pass.student.id == student.Id).timeStamp,
                        HasConflicts = details.studentConflicts.Any(conflict => conflict.student.id == student.Id),
                        ConflictCount = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id).conflictCount,
                        RedFlag = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id).redFlag,
                        PassAvailable = details.studentsWithPassAvailable.Any(stu => stu.id == student.Id)
                    })
            ];

            foreach (var student in Students)
                student.Selected += (_, selected) => StudentSelected?.Invoke(this, selected);

            Passess = LatePassViewModel.GetPasses(details);
            HasLatePasses = Passess.Any();

            Conflicts = [.. details.studentConflicts.Select(StudentConflictViewModel.Get)];

            HasConflicts = Conflicts.Any();
            HasRedFlags = Conflicts.Any(conflict => conflict.RedFlag);

            LoadComplete?.Invoke(this, EventArgs.Empty);
        });
    }

    public static AssessmentDetailsViewModel Get(AssessmentEntryRecord details)
    {
        var registrarService = ServiceHelper.GetService<RegistrarService>();
        var service = ServiceHelper.GetService<TeacherAssessmentService>();
        
        var vm = new AssessmentDetailsViewModel();

        var group = service.MyAssessments.FirstOrDefault(grp => grp.id == details.groupId);

        vm.Model = OptionalStruct<AssessmentCalendarEvent>.Some(details.ToCalendarEvent(group));

        vm.DateLabel = $"{details.assessmentDateTime:dddd dd MMMM hh:mm tt}";
        vm.Date = details.assessmentDateTime;
        vm.Title = string.IsNullOrEmpty(group.note) ? group.course : group.note;
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
                        Student = student,
                        LatePassUsed = details.studentsUsingPasses.Any(pass => pass.student.id == student.Id),
                        LatePassTimeStamp = details.studentsUsingPasses.FirstOrDefault(pass => pass.student.id == student.Id).timeStamp,
                        HasConflicts = details.studentConflicts.Any(conflict => conflict.student.id == student.Id),
                        ConflictCount = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id).conflictCount,
                        RedFlag = details.studentConflicts.FirstOrDefault(conflict => conflict.student.id == student.Id).redFlag,
                        PassAvailable = details.studentsWithPassAvailable.Any(stu => stu.id == student.Id)
                    })
        ];

        vm.Passess = LatePassViewModel.GetPasses(details);
        vm.HasLatePasses = vm.Passess.Any();

        vm.Conflicts = [.. details.studentConflicts.Select(StudentConflictViewModel.Get)];

        vm.HasConflicts = vm.Conflicts.Any();
        vm.HasRedFlags = vm.Conflicts.Any(conflict => conflict.RedFlag);
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
    IModelCarrier<StudentConflictViewModel, StudentConflictCount>
{
    private readonly RegistrarService _regsitrar = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty] UserViewModel student = UserViewModel.Empty;
    [ObservableProperty] int conflictCount;
    [ObservableProperty] bool latePassUsed;
    [ObservableProperty] bool redFlag;

    public OptionalStruct<StudentConflictCount> Model { get; private set; } = OptionalStruct<StudentConflictCount>.None();

    public StudentConflictViewModel(StudentConflictCount conflict)
    {
        Model = OptionalStruct<StudentConflictCount>.Some(conflict);
        Student = UserViewModel.Get(conflict.student.GetUserRecord(_regsitrar));
        ConflictCount = conflict.conflictCount;
        LatePassUsed = conflict.latePass;
        RedFlag = conflict.redFlag;
    }

    public static StudentConflictViewModel Get(StudentConflictCount model) => new(model);
}
