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
using System.Diagnostics;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;
public partial class AssessmentGroupViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    ISelectable<AssessmentGroupViewModel>
{
    private readonly TeacherAssessmentService _assessmentService = ServiceHelper.GetService<TeacherAssessmentService>();
    private AssessmentGroup _group;

    public event EventHandler? LoadCompleted;
    public event EventHandler? Deleted;
    public event EventHandler? Saved;
    public event EventHandler? Created;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AssessmentGroupViewModel>? Selected;
    public event EventHandler<AssessmentDetailsViewModel>? SectionSelected;
    public event EventHandler<AssessmentDetailsViewModel>? ShowDetailsRequested;
    public event EventHandler<AssessmentDetailsViewModel>? ShowDetailsPageRequested;
    public event EventHandler<StudentAssessmentRosterEntry>? StudentSelected;

    public static readonly AssessmentGroupViewModel Empty = new();

    [ObservableProperty] CourseViewModel course = CourseViewModel.Empty;
    [ObservableProperty] string note = "";
    [ObservableProperty] ObservableCollection<AssessmentEditorViewModel> assessments = [];
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool isNew;
    [ObservableProperty] string label = "";

    private AssessmentGroupViewModel() { }

    public static async Task<AssessmentGroupViewModel> CreateFor(CourseViewModel course)
    {
        course.MySectionsOnly = true;
        AssessmentGroupViewModel vm = new() { Course = course, IsSelected = true, IsNew = true };
       
        await vm.Course.LoadSections();
        vm.Assessments = [.. vm.Course.CurrentSections.Select(AssessmentEditorViewModel.Create)];
        foreach(var entry in vm.Assessments)
        {
            entry.ShowDetailsRequested += (sender, details) => vm.ShowDetailsRequested?.Invoke(sender, details);
            entry.StudentSelected += (sender, e) => vm.StudentSelected?.Invoke(vm, e);
        }
        return vm;
    }

    public AssessmentGroupViewModel(AssessmentGroup group)
    {
        _group = group;
        Note = _group.note;
        var course = _assessmentService.CourseList.FirstOrDefault(course => course.courseId == group.courseId);
        if(course is null)
        {
            _assessmentService.ClearCache();
            var reinit = _assessmentService.Initialize(OnError.DefaultBehavior(this));
            reinit.WhenCompleted(() =>
            {
                course = _assessmentService.CourseList.FirstOrDefault(course => course.courseId == group.courseId);
                if (course is null)
                {
                    throw new UnreachableException();
                }


                Course = CourseViewModel.Get(course);
                Label = string.IsNullOrEmpty(Note) ? Course.DisplayName : $"{Course.DisplayName} - {Note}";
                LoadGroup().SafeFireAndForget(e => e.LogException());
            });

            return;
        }
        Course = CourseViewModel.Get(course);
        Label = string.IsNullOrEmpty(Note) ? Course.DisplayName : $"{Course.DisplayName} - {Note}";
        LoadGroup().SafeFireAndForget(e => e.LogException());
    }

    [RelayCommand]
    private async Task LoadGroup()
    {
        Note = _group.note;
        await Course.LoadSections();
        Assessments = [.. Course.CurrentSections.Select(AssessmentEditorViewModel.Create)];

        foreach (var entry in Assessments)
        {
            entry.ShowDetailsRequested += (sender, details) => ShowDetailsRequested?.Invoke(sender, details);
            entry.Selected += (sender, details) => ShowDetailsPageRequested?.Invoke(sender, details);
            entry.StudentSelected += (sender, e) => StudentSelected?.Invoke(this, e);
        }

        foreach (var entry in _group.assessments)
        {
            var detail = await _assessmentService.GetAssessmentDetails(entry.assessmentId, OnError.DefaultBehavior(this));
            if (detail is not null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var vm = Assessments.FirstOrDefault(ent => ent.Section.Model.Reduce(SectionRecord.Empty).sectionId == detail.section.sectionId);
                    if (vm is null)
                        return;
                    vm.Model = Optional<AssessmentEntryRecord>.Some(detail);
                    vm.LoadDetails();
                    vm.IsSelected = true;
                    vm.Date = detail.assessmentDateTime;
                });
            }
        }

        LoadCompleted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Refreshing";
        await LoadGroup();
        Busy = false;
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
            .Select(ent => new AssessmentDateRecord(ent.Section.Id, ent.Date))
            .ToList(), Note);

        var result = await _assessmentService.CreateNewAssessment(update, OnError.DefaultBehavior(this));

        if (result is not null)
        {
            _group = result;
            IsNew = false;
            Label = string.IsNullOrEmpty(Note) ? Course.DisplayName : $"{Course.DisplayName} - {Note}";
            LoadGroup().SafeFireAndForget(e => e.LogException());
            Created?.Invoke(this, EventArgs.Empty);
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
            .Select(ent => new AssessmentDateRecord(ent.Model.Reduce(AssessmentEntryRecord.Empty).section.sectionId, ent.Date))
            .ToList(), Note);

        var result = await _assessmentService.UpdateAssessment(_group.id, update, OnError.DefaultBehavior(this));

        if (result is not null)
        {
            _group = result;
            Label = string.IsNullOrEmpty(Note) ? Course.DisplayName : $"{Course.DisplayName} - {Note}";
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
    public Optional<AssessmentEntryRecord> Model { get; set; } = Optional<AssessmentEntryRecord>.None();

    [ObservableProperty] AssessmentDetailsViewModel details = new();
    [ObservableProperty] bool hasLatePasses;
    [ObservableProperty] bool hasConflicts;
    [ObservableProperty] bool hasRedFlags;

    [ObservableProperty] SectionViewModel section = SectionViewModel.Empty;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool isSelected;

    [ObservableProperty] bool isInitalized;


    [ObservableProperty]
    DateTime date;

    public event EventHandler<AssessmentDetailsViewModel>? Selected;
    public event EventHandler<AssessmentDetailsViewModel>? ShowDetailsRequested;
    public event EventHandler<StudentAssessmentRosterEntry>? StudentSelected;
    public event EventHandler<ErrorRecord>? OnError;
    public AssessmentEditorViewModel(AssessmentEntryRecord entry)
    {
        if (string.IsNullOrEmpty(entry.assessmentId))
        {
            isInitalized = false;
            return;
        }

        if(entry == default)
        {
            LoadDetails();
            entry = Model.Reduce(AssessmentEntryRecord.Empty);
        }
        else
            Model = Optional<AssessmentEntryRecord>.Some(entry);
        Details = AssessmentDetailsViewModel.Get(entry);
        Details.StudentSelected += (sender, e) => StudentSelected?.Invoke(this, e);
        HasConflicts = entry.studentConflicts.Count != 0;
        HasLatePasses = entry.studentsUsingPasses.Count != 0;
        HasRedFlags = entry.studentConflicts.Any(conflict => conflict.redFlag);
        Section = SectionViewModel.Get(entry.section);
        date = entry.assessmentDateTime;
    }

    private AssessmentEditorViewModel(SectionViewModel section) 
    {
        Section = section;
        Date = DateTime.Today;
    }
    public static AssessmentEditorViewModel Create(SectionViewModel section) => new(section);

    public static AssessmentEditorViewModel Get(AssessmentEntryRecord model) => new(model);

    [RelayCommand]
    public void LoadDetails()
    {
        if (string.IsNullOrEmpty(Model.Reduce(AssessmentEntryRecord.Empty).assessmentId)) return;

        IsInitalized = true;
        HasConflicts = Model.Reduce(AssessmentEntryRecord.Empty).studentConflicts.Count != 0;
        HasLatePasses = Model.Reduce(AssessmentEntryRecord.Empty).studentsUsingPasses.Count != 0;
        HasRedFlags = Model.Reduce(AssessmentEntryRecord.Empty).studentConflicts.Any(conflict => conflict.redFlag);
        Details = AssessmentDetailsViewModel.Get(Model.Reduce(AssessmentEntryRecord.Empty));
        Details.StudentSelected += (sender, e) => StudentSelected?.Invoke(this, e);
    }

    [RelayCommand]
    public void ShowDetails() => ShowDetailsRequested?.Invoke(this, Details);

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
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] ObservableCollection<AssessmentGroupViewModel> myAssessmentGroups = [];
    [ObservableProperty] AssessmentGroupViewModel selectedAssessmentGroup = AssessmentGroupViewModel.Empty;
    [ObservableProperty] bool showSelectedGroup;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AssessmentDetailsViewModel>? ShowDetailsRequested;
    public event EventHandler<AssessmentDetailsViewModel>? ShowDetailsPageRequested;
    public event EventHandler<StudentAssessmentRosterEntry>? StudentSelected;

    public async Task Initialize(ErrorAction onError)
    {
        await _service.WaitForInit(onError);

        await Task.Delay(1000);
        
        MyAssessmentGroups = 
        [.. 
            _service.MyAssessments
            .OrderBy(grp => grp.assessments.Select(entry => entry.assessmentDateTime).Min())
            .Select(grp => new AssessmentGroupViewModel(grp))
        ];

        foreach(var assessment in MyAssessmentGroups.SelectMany(grp => grp.Assessments))
            assessment.LoadDetails();
        
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

            group.ShowDetailsRequested += (sender, e) => ShowDetailsRequested?.Invoke(group, e);
            group.ShowDetailsPageRequested += (sender, e) => ShowDetailsPageRequested?.Invoke(sender, e);
            group.StudentSelected += (sender, e) => StudentSelected?.Invoke(sender, e);
        }
    }

    public async Task AddGroupFor(CourseViewModel course)
    {
        Busy = true;
        BusyMessage = $"Starting Assessment for {course.DisplayName}";
        var newGroup = await AssessmentGroupViewModel.CreateFor(course);

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

        newGroup.Created += (_, _) =>
            MyAssessmentGroups.Add(newGroup);

        newGroup.ShowDetailsRequested += (sender, e) => ShowDetailsRequested?.Invoke(newGroup, e);

        newGroup.StudentSelected += (sender, e) => StudentSelected?.Invoke(sender, e);

        SelectedAssessmentGroup = newGroup;
        ShowSelectedGroup = true;
        Busy = false;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        using DebugTimer _ = new("Refreshing My Assessments", _logging);
        Busy = true;
        BusyMessage = "Refreshing";
        //await _service.Refresh(OnError.DefaultBehavior(this));
        await _service.GetMyAssessments(OnError.DefaultBehavior(this), DateTime.Today, DateTime.Today.AddMonths(6));
        await Initialize(OnError.DefaultBehavior(this));

        foreach (var group in MyAssessmentGroups)
            await group.Refresh();

        Busy = false;
    }
}

