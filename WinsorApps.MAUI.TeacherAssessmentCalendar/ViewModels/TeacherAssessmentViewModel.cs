using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Global;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class AssessmentDetailsViewModel : 
    ObservableObject,
    IModelCarrier<AssessmentDetailsViewModel, AssessmentCalendarEvent>,
    IErrorHandling
{
    public event EventHandler? LoadComplete;
    public event EventHandler<ErrorRecord>? OnError;

    public AssessmentCalendarEvent Model { get; private set; }
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
    string listLabel = "";

    [ObservableProperty]
    ObservableCollection<string> classList = [];

    [ObservableProperty]
    ObservableCollection<UserViewModel> students = [];

    [ObservableProperty]
    ObservableCollection<StudentConflictViewModel> conflicts = [];

    [ObservableProperty]
    ObservableCollection<LatePassViewModel> passess = [];

    public AssessmentDetailsViewModel() { }

    public AssessmentDetailsViewModel(AssessmentCalendarEvent @event)
    {
        Model = @event;
        title = Model.summary;
        subtitle = "";
        dateLabel = Model.allDay ? $"{Model.start:dddd, dd MMMM}" : $"{Model.start:dddd, dd MMM h:mm tt}";

        switch (Model.type)
        {
            case "assessment":
                LoadAssessmentDetails();
                break;
            case "note":
                listLabel = "Classes";
                classList = [.. Model.description.Split(';')];
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
        var getTask = _calendarService.GetAPExamDetails(Model.id, OnError.DefaultBehavior(this));
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
            Students = [ .. 
                UserViewModel.GetClonedViewModels(
                    _registrarService.StudentList
                    .Where(student => exam.studentIds.Contains(student.id)))
                ];

            LoadComplete?.Invoke(this, EventArgs.Empty);
        });
    }

    private void LoadAssessmentDetails()
    {
        var getTask = _assessmentService.GetAssessmentDetails(Model.id, OnError.DefaultBehavior(this));
        getTask.WhenCompleted(() =>
        {
            var result = getTask.Result;
            if (!result.HasValue)
            {
                Title = "Unable to Load Assessment";
                return;
            }

            var details = result.Value;
            Title = string.IsNullOrEmpty(Model.description) ? Model.summary : Model.description;
            Subtitle = $"{details.section.displayName} [{details.section.teachers
                    .Select(t => $"{t}")
                    .Aggregate((a, b) => $"{a}, {b}")}]";
            ListLabel = "Students";
        Students = [.. 
                    UserViewModel.GetClonedViewModels(
                        _registrarService.StudentList
                        .Where(student => details.section.students.Any(stu => stu.id == student.id)))
                ];
            details.section.students
                .ToImmutableArray();

            Passess = LatePassViewModel.GetPasses(details);

            Conflicts = [.. details.studentConflicts.Select(StudentConflictViewModel.Get)];

            LoadComplete?.Invoke(this, EventArgs.Empty);
        });
    }

    public static AssessmentDetailsViewModel Get(AssessmentCalendarEvent model) => new(model);
}


public partial class StudentConflictViewModel : 
    ObservableObject,
    IModelCarrier<StudentConflictViewModel, StudentConflictCount>
{
    private readonly RegistrarService _regsitrar = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty] UserViewModel student = UserViewModel.Empty;
    [ObservableProperty] int conflictCount;
    public StudentConflictCount Model { get; private set; }

    public StudentConflictViewModel(StudentConflictCount conflict)
    {
        Model = conflict;
        Student = UserViewModel.Get(conflict.student.GetUserRecord(_regsitrar));
        ConflictCount = conflict.count;
    }

    public static StudentConflictViewModel Get(StudentConflictCount model) => new(model);
}
