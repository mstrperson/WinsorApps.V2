using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class LateWorkPageViewModel(TeacherAssessmentService service, RegistrarService registrar) :
    ObservableObject,
    IBusyViewModel,
    IErrorHandling
{
    private readonly TeacherAssessmentService _service = service;
    private readonly RegistrarService _registrar = registrar;

    [ObservableProperty] ObservableCollection<SectionLateWorkCollection> sections = [];
    [ObservableProperty] SectionLateWorkCollection selectedSection = new();
    [ObservableProperty] bool showSelectedSection;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;

    public async Task Initialize(ErrorAction onError)
    {
        await _service.WaitForInit(onError);
        await _registrar.WaitForInit(onError);

        var myClasses = _registrar.MyAcademicSchedule;
        Sections = [.. myClasses.Select(sec => new SectionLateWorkCollection(SectionViewModel.Get(sec)))];
        foreach(var section in Sections)
        {
            section.OnError += (sender, e) => OnError?.Invoke(sender, e);
            section.Selected += (_, _) =>
            {
                ShowSelectedSection = section.IsSelected;
                foreach(var sec in Sections)
                {
                    sec.IsSelected = sec.Section.Model.Reduce(SectionRecord.Empty).sectionId == section.Section.Model.Reduce(SectionRecord.Empty).sectionId && section.IsSelected;
                }
                SelectedSection = section.IsSelected ? section : new();
            };

            section.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }
    }
}

public partial class LateWorkViewModel :
    ObservableObject,
    IModelCarrier<LateWorkViewModel, LateWorkDetails>,
    IErrorHandling
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();

    public event EventHandler? Resolved;
    public Optional<LateWorkDetails> Model { get; private set; } = Optional<LateWorkDetails>.None();

    [ObservableProperty] string id = "";
    [ObservableProperty] string details = "";
    [ObservableProperty] DateTime resolvedDate;
    [ObservableProperty] bool isResolved;
    [ObservableProperty] DateTime marked;
    [ObservableProperty] bool isAssessment;

    [ObservableProperty] AssessmentDetailsViewModel assessment = new();
    [ObservableProperty] SectionViewModel section = SectionViewModel.Empty;

    public event EventHandler<ErrorRecord>? OnError;

    public static async  Task<LateWorkViewModel> Get(LateWorkRecord model, ErrorAction onError)
    {
        var service = ServiceHelper.GetService<TeacherAssessmentService>();
        var details = await service.GetLateAssessmentDetails(onError, model.id);
        if(details is not null)
            return Get(details);
        return new();
    }

    public static LateWorkViewModel Get(LateWorkDetails model)
    {
        var vm = new LateWorkViewModel()
        {
            Model = Optional<LateWorkDetails>.Some(model),
            Id = model.id,
            Details = model.details,
            Marked = model.markedDates.FirstOrDefault(),
            IsResolved = model.isResolved,
            ResolvedDate = model.resolvedDate ?? default,
            IsAssessment = model.isAssessment
        };

        if(model.assessment is not null)
        {
            vm.Assessment = AssessmentDetailsViewModel.Get(model.assessment);
            vm.Section = SectionViewModel.Get(model.assessment.section);
        }

        if(model.section is not null)
        {
            vm.Section = SectionViewModel.Get(model.section);
        }

        return vm;
    }

    [RelayCommand]
    public async Task Resolve()
    {
        bool success = true;
        if (IsAssessment)
            await _service.ResolveLateAssessment(this.Id, err =>
            {
                success = false;
                OnError.DefaultBehavior(this)(err);
            });
        else
            await _service.ResolveLateWorkPattern(this.Id, err =>
            {
                success = false;
                OnError.DefaultBehavior(this)(err);
            });

        if (success)
        {
            Resolved?.Invoke(this, EventArgs.Empty);
            Model = Model.Map(model => model with { isResolved = true, resolvedDate=DateTime.Now });
            IsResolved = true;
            ResolvedDate = DateTime.Now;
        }
    }
}

public partial class StudentLateWorkCollectionViewModel : 
    ObservableObject,
    IErrorHandling
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();

    [ObservableProperty] StudentViewModel student = new();
    [ObservableProperty] int totalLateWork;
    [ObservableProperty] int outstandingLateWork;
    [ObservableProperty] ObservableCollection<LateWorkViewModel> lateWorkPatterns = [];
    [ObservableProperty] ObservableCollection<LateWorkViewModel> lateAssessments = [];
    [ObservableProperty] bool hasPatterns;
    [ObservableProperty] bool hasAssessments;
    [ObservableProperty] bool showLateWork;

    [ObservableProperty] private double patternHeight;
    [ObservableProperty] private double assessmentHeight;
    [ObservableProperty] private double pannelHeight = 150;

    private static readonly double LW_Header_Height = 80;
    private static readonly double LW_Row_Height = 60;
    private static readonly double LA_Row_Height = 85;

    public StudentLateWorkCollection Model { get; private set; }

    public StudentLateWorkCollectionViewModel(StudentLateWorkCollection lateWork)
    {
        Student = StudentViewModel.Get(lateWork.student);
        Model = lateWork;
        Load().SafeFireAndForget(e => e.LogException());
    }

    [RelayCommand]
    public void ToggleShowLateWork()
    {
        ShowLateWork = !ShowLateWork;
        PannelHeight = ShowLateWork switch
        {
            true => 150 + PatternHeight + AssessmentHeight,
            false => 150
        };
    }

    [RelayCommand]
    public async Task Load()
    {
        LateWorkPatterns = [];
        LateAssessments = [];
        var result = new List<LateWorkViewModel>();
        foreach(var lw in Model.lateWork)
        {
            var details = lw.isAssessment switch
            {
                true => await _service.GetLateAssessmentDetails(OnError.DefaultBehavior(this), lw.id),
                false => await _service.GetLateWorkPattern(OnError.DefaultBehavior(this), lw.id)
            };

            if(details is not null)
                result.Add(LateWorkViewModel.Get(details));
        }

        LateWorkPatterns = [.. result.Where(lw => !lw.IsAssessment).OrderBy(lw => lw.Marked)];
        HasPatterns = LateWorkPatterns.Any();
        PatternHeight = LW_Header_Height + LateWorkPatterns.Count * LW_Row_Height;
        LateAssessments = [.. result.Where(lw => lw.IsAssessment).OrderBy(lw => lw.Marked)];
        HasAssessments = LateAssessments.Any();
        AssessmentHeight = LW_Header_Height + LateAssessments.Count * LA_Row_Height;
        TotalLateWork = Model.lateWork.Count;
        OutstandingLateWork = Model.lateWork.Count(lw => !lw.isResolved);


        foreach (var lw in LateWorkPatterns.Union(LateAssessments))
        {
            lw.OnError += (sender, e) => OnError?.Invoke(sender, e);
        }
    }

    public event EventHandler<ErrorRecord>? OnError;
}

public partial class SectionLateWorkCollection :
    ObservableObject,
    IErrorHandling,
    ISelectable<SectionLateWorkCollection>,
    IBusyViewModel
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();
    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<SectionLateWorkCollection>? Selected;

    [ObservableProperty] SectionViewModel section = SectionViewModel.Empty;
    [ObservableProperty] ObservableCollection<StudentLateWorkCollectionViewModel> lateWorkByStudent = [];
    [ObservableProperty] bool showResolved;
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] ObservableCollection<AssessmentEditorViewModel> assessments = [];
    [ObservableProperty] bool showAssessments;
    [ObservableProperty] bool assessmentSelected;
    [ObservableProperty] CreateLateAssessmentViewModel createLateAssessment = CreateLateAssessmentViewModel.Empty;
    [ObservableProperty] bool showNewLateAssessment;
    [ObservableProperty] CreateLateWorkPatternViewModel createPattern = new();
    [ObservableProperty] bool showNewPattern;
    [ObservableProperty] bool showLateWork = true;

    public SectionLateWorkCollection() { }

    public SectionLateWorkCollection(SectionViewModel section)
    {
        Section = section;
        LoadLateWork().SafeFireAndForget(e => e.LogException());
        LoadAssessments().SafeFireAndForget(e => e.LogException());
        CreateLateAssessment.Submitted += async (_, _) => await LoadLateWork();
        CreateLateAssessment.OnError += (sender, e) => OnError?.Invoke(sender, e);
        CreatePattern = new(Section);
        CreatePattern.Submitted += async (_, _) => await LoadLateWork();
    }

    [RelayCommand]
    public void ToggleShowLateWork()
    {
        ShowLateWork = !ShowLateWork;
        ShowAssessments = !ShowLateWork;
        if (ShowLateWork)
        {
            ShowNewLateAssessment = false;
            ShowNewPattern = false;
        }
    }

    [RelayCommand]
    public async Task LoadAssessments()
    {
        await _service.WaitForInit(OnError.DefaultBehavior(this));

        var schoolYear = _registrar.SchoolYears.First(sy => sy.startDate <= DateOnly.FromDateTime(DateTime.Today) && sy.endDate >= DateOnly.FromDateTime(DateTime.Today));

        var assessments = await _service.GetAssessmentsFor(OnError.DefaultBehavior(this),
            Section.Model.Reduce(SectionRecord.Empty).sectionId, schoolYear.startDate.ToDateTime(default), schoolYear.endDate.ToDateTime(default));

        Assessments = [.. assessments
            .Where(entry => 
                entry.section.sectionId == Section.Model.Reduce(SectionRecord.Empty).sectionId)
            .Select(AssessmentEditorViewModel.Get)];

        foreach(var assessment in Assessments)
        {
            assessment.OnError += (sender, e) => OnError?.Invoke(sender, e);
            assessment.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            assessment.Selected += (_, _) =>
            {
                ShowNewLateAssessment = assessment.IsSelected;
                ShowAssessments = !assessment.IsSelected;
                CreateLateAssessment = new(assessment);
                CreateLateAssessment.Submitted += async (_, _) =>
                {
                    await LoadLateWork();
                    ShowNewLateAssessment = false;
                    ShowAssessments = true;
                };
                foreach(var other in Assessments)
                {
                    other.IsSelected = other.Model.Reduce(AssessmentEntryRecord.Empty).assessmentId == assessment.Model.Reduce(AssessmentEntryRecord.Empty).assessmentId && assessment.IsSelected;
                }
            };
        }
    }

    [RelayCommand]
    public async Task ToggleShowAssessments()
    {
        ShowAssessments = !ShowAssessments;
        ShowNewLateAssessment = false;
        ShowNewPattern = false;
        ShowLateWork = !ShowAssessments;
    }

    [RelayCommand]
    public void ToggleShowNewPattern()
    {
        ShowNewPattern = !ShowNewPattern;
        ShowNewLateAssessment = false;
        ShowAssessments = !ShowNewPattern;
        ShowLateWork = false;
    }

    [RelayCommand]
    public async Task ToggleShowResolved()
    {
        ShowResolved = !ShowResolved;
        await LoadLateWork();
    }

    [RelayCommand]
    public async Task LoadLateWork()
    {
        if (string.IsNullOrEmpty(Section.Model.Reduce(SectionRecord.Empty).sectionId))
            return;
        Busy = true;
        BusyMessage = "Loading Late Work...";
        var latework = await _service.GetLateWorkBySection(OnError.DefaultBehavior(this), Section.Model.Reduce(SectionRecord.Empty).sectionId, ShowResolved);
        LateWorkByStudent = [.. latework.Select(lwc => new StudentLateWorkCollectionViewModel(lwc))];
        Busy = false;
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class CreateLateWorkPatternViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<CreateLateWorkPatternViewModel>? Submitted;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] SectionViewModel section = new();

    public CreateLateWorkPatternViewModel()
    {

    }

    public CreateLateWorkPatternViewModel(SectionViewModel section)
    {
        this.section = section;

        NotSelectedStudents = [.. section.Students.Select(user => user.Clone())];
        foreach (var student in NotSelectedStudents)
        {
            student.Selected += (_, _) =>
            {
                var allStudents = SelectedStudents.Union(NotSelectedStudents);
                SelectedStudents = [.. allStudents.Where(stu => stu.IsSelected)];
                NotSelectedStudents = [.. allStudents.Where(stu => !stu.IsSelected)];
            };
        }
    }

    [ObservableProperty] string details = "";
    [ObservableProperty] ObservableCollection<UserViewModel> selectedStudents = [];
    [ObservableProperty] ObservableCollection<UserViewModel> notSelectedStudents = []; 
    
    [RelayCommand]
    public async Task Submit()
    {
        _ = await _service.PostLateWorkPatternFor(OnError.DefaultBehavior(this), Section.Model.Reduce(SectionRecord.Empty).sectionId,
            new(Details, [.. SelectedStudents.Select(student => student.Id)]));
        Submitted?.Invoke(this, this);
        Details = "";
        NotSelectedStudents = [.. SelectedStudents.Union(NotSelectedStudents)];
        SelectedStudents = [];
    }
}

public partial class CreateLateAssessmentViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    ISelectable<CreateLateAssessmentViewModel>
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();

    public static CreateLateAssessmentViewModel Empty => new();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<CreateLateAssessmentViewModel>? Selected;
    public event EventHandler<CreateLateAssessmentViewModel>? Submitted;

    [ObservableProperty] AssessmentEditorViewModel assessment;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool isSelected;

    [ObservableProperty] string details = "";
    [ObservableProperty] ObservableCollection<UserViewModel> selectedStudents = [];
    [ObservableProperty] ObservableCollection<UserViewModel> notSelectedStudents = [];

    public static implicit operator CreateLateAssessmentViewModel(AssessmentEditorViewModel assessment) => new(assessment);

    private CreateLateAssessmentViewModel()
    {
        assessment = new(AssessmentEntryRecord.Empty);
    }

    public CreateLateAssessmentViewModel(AssessmentEditorViewModel assessment)
    {
        this.assessment = assessment;
        NotSelectedStudents = [.. assessment.Section.Students.Select(user => user.Clone())];
        foreach(var student in NotSelectedStudents)
        {
            student.Selected += (_, _) =>
            {
                var allStudents = SelectedStudents.Union(NotSelectedStudents);
                SelectedStudents = [.. allStudents.Where(stu => stu.IsSelected)];
                NotSelectedStudents = [.. allStudents.Where(stu => !stu.IsSelected)];
            };
        }
    }

    [RelayCommand]
    public async Task Submit()
    {
        _ = await _service.PostNewLateAssessment(OnError.DefaultBehavior(this), Assessment.Model.Reduce(AssessmentEntryRecord.Empty).assessmentId,
            new(Details, [.. SelectedStudents.Select(student => student.Id)]));
        Submitted?.Invoke(this, this);
        Details = "";
        NotSelectedStudents = [.. SelectedStudents.Union(NotSelectedStudents)];
        SelectedStudents = [];
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

