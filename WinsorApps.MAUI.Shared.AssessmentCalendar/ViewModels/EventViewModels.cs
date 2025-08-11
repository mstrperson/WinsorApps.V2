using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;


public partial class AssessmentCalendarEventViewModel :
    ObservableObject,
    IModelCarrier<AssessmentCalendarEventViewModel, AssessmentCalendarEvent>,
    ISelectable<AssessmentCalendarEventViewModel>,
    IErrorHandling
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private AssessmentType type = AssessmentType.Assessment;
    [ObservableProperty] private string summary = "";
    [ObservableProperty] private string description = "";
    [ObservableProperty] private DateTime start;
    [ObservableProperty] private DateTime end;
    [ObservableProperty] private bool allDay;
    [ObservableProperty] private ObservableCollection<StudentClassName> affectedClasses = [];
    [ObservableProperty] private bool passUsed;
    [ObservableProperty] private bool passAvailable;
    [ObservableProperty] private bool isSelected;

    public AssessmentEntryRecord? Details { get; private set; } = null;

    public Optional<AssessmentCalendarEvent> Model { get; private set; } = Optional<AssessmentCalendarEvent>.None();


    public event EventHandler<AssessmentCalendarEventViewModel>? Selected;
    public event EventHandler<ErrorRecord>? OnError;

    public static AssessmentCalendarEventViewModel Get(AssessmentCalendarEvent model)
    {
        var vm = new AssessmentCalendarEventViewModel()
        {
            Id = model.id,
            Type = model.type,
            Summary = model.summary,
            Description = model.description,
            Start = model.start,
            End = model.end,
            AllDay = model.allDay,
            AffectedClasses = [.. model.affectedClasses],
            PassUsed = model.passUsed ?? false,
            PassAvailable = model.passAvailable ?? false,
            Model = Optional<AssessmentCalendarEvent>.Some(model)
        };

        //vm.LoadAssessmentDetails().SafeFireAndForget(e => e.LogException());

        return vm;
    }

    [RelayCommand]
    public async Task LoadAssessmentDetails()
    {
        if (Type == AssessmentType.Assessment)
        {
            var teacherService = ServiceHelper.GetService<TeacherAssessmentService>();
            if (teacherService is null)
                return;

            Details = await teacherService.GetAssessmentDetails(Model.Reduce(AssessmentCalendarEvent.Empty).id, OnError.DefaultBehavior(this));

            if(Details is not null)
            {
                Description += $" [{Details.section.teachers.First().lastName}]";
            }
        }
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class ApExamViewModel :
    ObservableObject,
    IModelCarrier<ApExamViewModel, APExamDetail>
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string courseName = "";
    [ObservableProperty] private DateTime start;
    [ObservableProperty] private DateTime end;
    [ObservableProperty] private ObservableCollection<SectionViewModel> sections = [];
    [ObservableProperty] private ObservableCollection<UserViewModel> students = [];

    public Optional<APExamDetail> Model { get; private set; } = Optional<APExamDetail>.None();

    public CreateAPExam ToCreateRecord() => new(CourseName, Start, End, [.. Sections.Select(sec => sec.Model.Reduce(SectionRecord.Empty).sectionId)], [.. Students.Select(stu => stu.Id)]);

    public static ApExamViewModel Get(APExamDetail model)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();

        ApExamViewModel item = new()
        {
            Id = model.id,
            CourseName = model.courseName,
            Start = model.startDateTime,
            End = model.endDateTime,
            Sections = [.. model.sectionIds.Select(id => SectionViewModel.Get(registrar.SectionDetailCache[id]))],
            Students = [.. model.studentIds
                .Select(id => UserViewModel.Get(registrar.StudentList.FirstOrDefault(u => u.id == id) ?? UserRecord.Empty))
                .Where(student => !string.IsNullOrEmpty(student.Id))],
            Model = Optional<APExamDetail>.Some(model)
        };

        return item;
    }
}

public partial class DayNoteViewModel :
    ObservableObject,
    IModelCarrier<DayNoteViewModel, DayNote>
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private DateOnly date;
    [ObservableProperty] private string note = "";
    [ObservableProperty] private List<StudentClassName> affectedClasses = [];

    public Optional<DayNote> Model { get; private set; } = Optional<DayNote>.None();

    public static DayNoteViewModel Get(DayNote model) => new()
    {
        Id = model.id,
        Date = model.date,
        Note = model.note,
        AffectedClasses = [.. model.affectedClasses],
        Model = Optional<DayNote>.Some(model)
    };
}


