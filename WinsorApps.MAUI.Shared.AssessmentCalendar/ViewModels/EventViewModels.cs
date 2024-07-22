global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;


public partial class AssessmentCalendarEventViewModel :
    ObservableObject,
    IModelCarrier<AssessmentCalendarEventViewModel, AssessmentCalendarEvent>,
    ISelectable<AssessmentCalendarEventViewModel>
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

    public AssessmentCalendarEvent Model => new(Id, Type, Summary, Description, Start, End, AllDay, [.. AffectedClasses], PassUsed, PassAvailable);


    public event EventHandler<AssessmentCalendarEventViewModel>? Selected;

    public static AssessmentCalendarEventViewModel Get(AssessmentCalendarEvent model) => new()
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
        PassAvailable = model.passAvailable ?? false
    };

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

    public APExamDetail Model { get; private set; }

    public CreateAPExam ToCreateRecord() => new(CourseName, Start, End, [.. Sections.Select(sec => sec.Model.sectionId)], [.. Students.Select(stu => stu.Id)]);

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
            Model = model
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

    public DayNote Model => new(Id, Date, Note, [.. AffectedClasses]);

    public static DayNoteViewModel Get(DayNote model) => new()
    {
        Id = model.id,
        Date = model.date,
        Note = model.note,
        AffectedClasses = [.. model.affectedClasses]
    };
}


