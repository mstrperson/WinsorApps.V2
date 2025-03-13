using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;

public partial class LatePassViewModel :
    ObservableObject,
    IModelCarrier<LatePassViewModel, AssessmentPassDetail>
{
    [ObservableProperty] string courseName = "";
    [ObservableProperty] string note = "";
    [ObservableProperty] DateTime dateAndTime;
    [ObservableProperty] DateTime timestamp;
    [ObservableProperty] UserViewModel student = UserViewModel.Empty;
    [ObservableProperty] MakeupTimeViewModel makeupTime = MakeupTimeViewModel.Empty;

    [ObservableProperty] FreeBlockCollectionViewModel freeBlockLookup = new();
    [ObservableProperty] bool showFreeBlockLookup;
    [ObservableProperty] bool showManualInput;

    public Optional<AssessmentPassDetail> Model { get; private set; } = Optional<AssessmentPassDetail>.None();

    public event EventHandler<AssessmentPassDetail>? LoadAssessmentRequested;

    public static LatePassViewModel CreateEmpty(UserViewModel student, AssessmentCalendarEventViewModel assessment)
    {
        var vm = new LatePassViewModel
        {
            Student = student,
            DateAndTime = assessment.Start,
            CourseName = assessment.Details?.section.displayName ?? assessment.Description
        };
        vm.FreeBlockLookup.User = student;

        var start = assessment.Start.Date.AddDays(1);
        while (start is { DayOfWeek: DayOfWeek.Sunday or DayOfWeek.Saturday })
            start = start.AddDays(1);

        var end = start.AddDays(2);
        while (end is { DayOfWeek: DayOfWeek.Sunday or DayOfWeek.Saturday })
            end = end.AddDays(1);
        vm.FreeBlockLookup.Start = start;
        vm.FreeBlockLookup.End = end;
        vm.FreeBlockLookup.User = vm.Student;

        vm.FreeBlockLookup.FreeBlockSelected += (_, block) =>
        {
            vm.MakeupTime = MakeupTimeViewModel.Get(new(block.Start, $"Make up during {block.BlockName} Block: {block.Start:dddd, d MMMM hh:mm tt}"));
            vm.ShowFreeBlockLookup = false;
        };

        return vm;
    }


    protected LatePassViewModel() { }

    public static ObservableCollection<LatePassViewModel> GetPasses(AssessmentEntryRecord assessment)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        ObservableCollection<LatePassViewModel> passes = [];
        foreach (var latePass in assessment.studentsUsingPasses)
        {
            var student = UserViewModel.Get(latePass.student.GetUserRecord(registrar));

            passes.Add(new()
            {
                Student = student,
                CourseName = assessment.section.displayName,
                Timestamp = latePass.timeStamp,
                Model = Optional<AssessmentPassDetail>.Some(
                    new(new(assessment.assessmentId, AssessmentType.Assessment, "", "",
                    assessment.assessmentDateTime, assessment.assessmentDateTime, false, []),
                    student.Model.Reduce(UserRecord.Empty), latePass.timeStamp, new()))
            });
        }

        return passes;
    }

    public static LatePassViewModel Get(AssessmentPassDetail model)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        var vm = new LatePassViewModel()
        {
            CourseName = registrar.CourseList.FirstOrDefault(course => course.courseCode == model.assessment.summary)?.displayName ?? "",
            Note = model.assessment.description,
            DateAndTime = model.assessment.start,
            Timestamp = model.timeStamp,
            Student = UserViewModel.Get(model.student),
            Model = Optional<AssessmentPassDetail>.Some(model)
        };

        var start = model.assessment.start.Date.AddDays(1);
        while (start is { DayOfWeek: DayOfWeek.Sunday or DayOfWeek.Saturday })
            start = start.AddDays(1);

        var end = start.AddDays(2);
        while (end is { DayOfWeek: DayOfWeek.Sunday or DayOfWeek.Saturday })
            end = end.AddDays(1);

        vm.FreeBlockLookup.Start = start;
        vm.FreeBlockLookup.End = end;
        vm.FreeBlockLookup.User = vm.Student;

        vm.FreeBlockLookup.FreeBlockSelected += (_, block) =>
        {
            vm.MakeupTime = MakeupTimeViewModel.Get(new(block.Start, $"Make up during {block.BlockName} Block: {block.Start:dddd, d MMMM hh:mm tt}"));
            vm.ShowFreeBlockLookup = false;
        };

        return vm;
    }

    [RelayCommand]
    public async Task ToggleShowFreeBlockLookup()
    {
        ShowFreeBlockLookup = !ShowFreeBlockLookup;
        if (ShowFreeBlockLookup)
            await FreeBlockLookup.LoadFreeBlocks();
    }

    [RelayCommand]
    public void ToggleManualInput()
    {
        ShowManualInput = !ShowManualInput;
        if (ShowManualInput)
            ShowFreeBlockLookup = false;
    }

    [RelayCommand]
    public void LoadAssessment() => LoadAssessmentRequested?.Invoke(this, Model.Reduce(AssessmentPassDetail.Empty));
}


public partial class MakeupTimeViewModel :
    ObservableObject,
    IErrorHandling,
    IDefaultValueViewModel<MakeupTimeViewModel>,
    IModelCarrier<MakeupTimeViewModel, MakeupTime>
{
    public static implicit operator MakeupTime(MakeupTimeViewModel model) => model switch
    {
        { IsScheduled: true } => new(model.Sheduled, model.Note),
        { Note: "" } => new(null, "Not Scheduled"),
        _ => new(null, model.Note)
    };

    public static MakeupTimeViewModel Empty => new() { Note = "Not Scheduled" };

    [ObservableProperty] DateTime sheduled = DateTime.Today;
    [ObservableProperty] string note = "";
    [ObservableProperty] bool isScheduled;

    public Optional<MakeupTime> Model { get; set; } = Optional<MakeupTime>.None();

    public event EventHandler<ErrorRecord>? OnError;

    public static MakeupTimeViewModel Get(MakeupTime model) => new()
    {
        Sheduled = model.makeupTime ?? DateTime.MaxValue,
        Note = model.note ?? string.Empty,
        IsScheduled = model.makeupTime is not null,
        Model = Optional<MakeupTime>.Some(model)
    };

}