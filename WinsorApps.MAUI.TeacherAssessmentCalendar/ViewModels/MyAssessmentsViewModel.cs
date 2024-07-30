using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;
public partial class AssessmentGroupViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    ISelectable<AssessmentGroupViewModel>
{
    private readonly TeacherAssessmentService _assessmentService = ServiceHelper.GetService<TeacherAssessmentService>();
    private readonly ReadonlyCalendarService _service = ServiceHelper.GetService<ReadonlyCalendarService>();
    private AssessmentGroup _group;

    public event EventHandler? LoadCompleted;
    public event EventHandler? Deleted;
    public event EventHandler? Saved;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AssessmentGroupViewModel>? Selected;
    public event EventHandler<AssessmentDetailsViewModel>? SectionSelected;

    public static AssessmentGroupViewModel Empty = new AssessmentGroupViewModel();

    [ObservableProperty] CourseViewModel course = CourseViewModel.Empty;
    [ObservableProperty] string note = "";
    [ObservableProperty] ObservableCollection<AssessmentEditorViewModel> assessments = [];
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool isNew;

    private AssessmentGroupViewModel() { }

    public static AssessmentGroupViewModel CreateFor(CourseViewModel course) => new() { Course = course, IsSelected = true, IsNew = true };

    public AssessmentGroupViewModel(AssessmentGroup group)
    {
        _group = group;
        var course = _assessmentService.CourseList.First(course => course.courseId == group.courseId);
        Course = CourseViewModel.Get(course);
        Assessments = [.. Course.Sections.Select(AssessmentEditorViewModel.Create)];
        Note = _group.note;
        LoadGroup().SafeFireAndForget(e => e.LogException());
    }

    [RelayCommand]
    private async Task LoadGroup()
    {
        Note = _group.note;
        Assessments = [];

        foreach (var entry in _group.assessments)
        {
            var detail = await _assessmentService.GetAssessmentDetails(entry.assessmentId, err => { });
            if (detail.HasValue)
            {
                var vm = Assessments.First(ent => ent.Section.Model.sectionId == detail.Value.section.sectionId);
                var model = _service.AssessmentCalendar.FirstOrDefault(ent => ent.id == entry.assessmentId && ent.type == AssessmentType.Assessment);
                if (model.id == entry.assessmentId)
                    vm.Details = AssessmentDetailsViewModel.Get(model);
                vm.IsSelected = true;
                vm.Date = detail.Value.assessmentDateTime;
            }
        }

        LoadCompleted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task Refresh()
    {
        await Task.WhenAll(Assessments.Select(async ent => await ent.Refresh()));
    }

    public async Task Create()
    {
        if (!IsNew)
        {
            await Update();
            return;
        }
        Busy = true;
        BusyMessage = $"Creating new Assessment in {Course.DisplayName}";
        var update = new CreateAssessmentRecord(
            Assessments
            .Where(ent => ent.IsSelected)
            .Select(ent => new AssessmentDateRecord(ent.Model.section.sectionId, ent.Date))
            .ToImmutableArray(), Note);

        var result = await _assessmentService.CreateNewAssessment(update, OnError.DefaultBehavior(this));

        if (result.HasValue)
        {
            _group = result.Value;
            IsNew = false;
            LoadGroup().SafeFireAndForget(e => e.LogException());
            Saved?.Invoke(this, EventArgs.Empty);
        }

        Busy = false;
    }

    [RelayCommand]
    public async Task Save() => await Update();

    [RelayCommand]
    public async Task Delete()
    {
        Busy = true;
        BusyMessage = $"Deleting {Note} from {Course.DisplayName}";
        await _assessmentService.DeleteAssessment(_group.id, OnError.DefaultBehavior(this));
        Deleted?.Invoke(this, EventArgs.Empty);
        Busy = false;
    }

    public async Task Update()
    {
        if (IsNew)
        {
            await Create();
            return;
        }
        Busy = true;
        BusyMessage = $"Updating {Note} in {Course.DisplayName}";

        var update = new CreateAssessmentRecord(
            Assessments
            .Where(ent => ent.IsSelected)
            .Select(ent => new AssessmentDateRecord(ent.Model.section.sectionId, ent.Date))
            .ToImmutableArray(), Note);

        var result = await _assessmentService.UpdateAssessment(_group.id, update, OnError.DefaultBehavior(this));

        if (result.HasValue)
        {
            _group = result.Value;
            LoadGroup().SafeFireAndForget(e => e.LogException());
            Saved?.Invoke(this, EventArgs.Empty);
        }
        Busy = false;
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class AssessmentEditorViewModel : 
    ObservableObject,
    IBusyViewModel,
    IModelCarrier<AssessmentEditorViewModel, AssessmentEntryRecord>,
    ISelectable<AssessmentDetailsViewModel>,
    IErrorHandling
{
    public readonly ReadonlyCalendarService _service = ServiceHelper.GetService<ReadonlyCalendarService>();
    public AssessmentEntryRecord Model { get; private set; }

    [ObservableProperty] AssessmentDetailsViewModel details = new();
    [ObservableProperty] SectionViewModel section;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool isSelected;

    [ObservableProperty]
    DateTime date;

    public event EventHandler<AssessmentDetailsViewModel>? Selected;
    public event EventHandler<ErrorRecord>? OnError;

    public AssessmentEditorViewModel(AssessmentEntryRecord entry)
    {
        Model = entry;
        var model = _service.AssessmentCalendar.First(ent => ent.id == entry.assessmentId && ent.type == AssessmentType.Assessment);
        if (model.id == entry.assessmentId)
            Details = AssessmentDetailsViewModel.Get(model);
        Section = SectionViewModel.Get(entry.section);
        date = entry.assessmentDateTime;
    }
    private AssessmentEditorViewModel(SectionViewModel section) 
    {
        Section = section;
        Model = new("", section.Model, default, [], []);
        Date = DateTime.Today;
    }
    public static AssessmentEditorViewModel Create(SectionViewModel section) => new AssessmentEditorViewModel(section);

    public static AssessmentEditorViewModel Get(AssessmentEntryRecord model) => new(model);

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Refreshing";
        var events = await _service.GetAssessmentCalendarOn(DateOnly.FromDateTime(Date), OnError.DefaultBehavior(this));

        var model = events.First(ent => ent.id == Model.assessmentId && ent.type == AssessmentType.Assessment);
        if (model.id == Model.assessmentId)
            Details = AssessmentDetailsViewModel.Get(model);
        Busy = false;
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, Details);
    }
}

public partial class MyAssessmentsCollectionViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();
    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty] ObservableCollection<AssessmentGroupViewModel> myAssessmentGroups = [];
    [ObservableProperty] AssessmentGroupViewModel selectedAssessmentGroup = AssessmentGroupViewModel.Empty;
    [ObservableProperty] bool showSelectedGroup;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;

    public async Task Initialize(ErrorAction onError)
    {
        await _service.WaitForInit(onError);
        MyAssessmentGroups = [.. _service.MyAssessments.Select(grp => new AssessmentGroupViewModel(grp))];
        foreach(var group in MyAssessmentGroups)
        {
            group.Deleted += (_, _) =>
            {
                MyAssessmentGroups.Remove(group);
            };

            group.Saved += (_, _) =>
            {
                group.IsSelected = false;
                SelectedAssessmentGroup = AssessmentGroupViewModel.Empty;
                ShowSelectedGroup = false;
            };

            group.Selected += (_, _) =>
            {
                ShowSelectedGroup = group.IsSelected;
                SelectedAssessmentGroup = ShowSelectedGroup ? group : AssessmentGroupViewModel.Empty;
            };

            group.OnError += (sender, e) => OnError?.Invoke(sender, e);
        }
    }

    public void AddGroupFor(CourseViewModel course)
    {
        var newGroup = AssessmentGroupViewModel.CreateFor(course);

        newGroup.Deleted += (_, _) =>
        {
            MyAssessmentGroups.Remove(newGroup);
        };

        newGroup.Saved += (_, _) =>
        {
            newGroup.IsSelected = false;
            SelectedAssessmentGroup = AssessmentGroupViewModel.Empty;
            ShowSelectedGroup = false;
        };

        newGroup.Selected += (_, _) =>
        {
            ShowSelectedGroup = newGroup.IsSelected;
            SelectedAssessmentGroup = ShowSelectedGroup ? newGroup : AssessmentGroupViewModel.Empty;
        };

        newGroup.OnError += (sender, e) => OnError?.Invoke(sender, e);

        MyAssessmentGroups.Add(newGroup);
        SelectedAssessmentGroup = newGroup;
        ShowSelectedGroup = true;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Refreshing";
        await Initialize(OnError.DefaultBehavior(this));
        await Task.WhenAll(MyAssessmentGroups.Select(async group => await group.Refresh()));
        Busy = false;
    }
}

