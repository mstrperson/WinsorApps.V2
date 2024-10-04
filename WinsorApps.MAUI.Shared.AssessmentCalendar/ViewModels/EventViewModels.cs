using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;
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
    [ObservableProperty] string id = "";
    [ObservableProperty] AssessmentType type = AssessmentType.Assessment;
    [ObservableProperty] string summary = "";
    [ObservableProperty] string description = "";
    [ObservableProperty] DateTime start;
    [ObservableProperty] DateTime end;
    [ObservableProperty] bool allDay;
    [ObservableProperty] ObservableCollection<StudentClassName> affectedClasses = [];
    [ObservableProperty] bool passUsed;
    [ObservableProperty] bool passAvailable;
    [ObservableProperty] bool isSelected;

    public OptionalStruct<AssessmentCalendarEvent> Model { get; private set; } = OptionalStruct<AssessmentCalendarEvent>.None();


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
            Model = OptionalStruct<AssessmentCalendarEvent>.Some(model)
        };

        vm.LoadAssessmentDetails().SafeFireAndForget(e => e.LogException());

        return vm;
    }

    [RelayCommand]
    public async Task LoadAssessmentDetails()
    {


        if (Type == AssessmentType.Assessment)
        {
            var teacherService = ServiceHelper.GetService<TeacherAssessmentService>();

            var details = await teacherService.GetAssessmentDetails(Model.Reduce(AssessmentCalendarEvent.Empty).id, OnError.DefaultBehavior(this));

            if(details.HasValue)
            {
                Description += $" [{details.Value.section.teachers.First().lastName}]";
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
    [ObservableProperty] string id = "";
    [ObservableProperty] string courseName = "";
    [ObservableProperty] DateTime start;
    [ObservableProperty] DateTime end;
    [ObservableProperty] ObservableCollection<SectionViewModel> sections = [];
    [ObservableProperty] ObservableCollection<UserViewModel> students = [];

    public OptionalStruct<APExamDetail> Model { get; private set; } = OptionalStruct<APExamDetail>.None();

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
            Students = [.. model.studentIds.Select(id => UserViewModel.Get(registrar.StudentList.FirstOrDefault(u => u.id == id)))],
            Model = OptionalStruct<APExamDetail>.Some(model)
        };

        return item;
    }
}

public partial class DayNoteViewModel :
    ObservableObject,
    IModelCarrier<DayNoteViewModel, DayNote>
{
    [ObservableProperty] string id = "";
    [ObservableProperty] DateOnly date;
    [ObservableProperty] string note = "";
    [ObservableProperty] ImmutableArray<StudentClassName> affectedClasses = [];

    public OptionalStruct<DayNote> Model { get; private set; } = OptionalStruct<DayNote>.None();

    public static DayNoteViewModel Get(DayNote model) => new()
    {
        Id = model.id,
        Date = model.date,
        Note = model.note,
        AffectedClasses = [.. model.affectedClasses],
        Model = OptionalStruct<DayNote>.Some(model)
    };
}


