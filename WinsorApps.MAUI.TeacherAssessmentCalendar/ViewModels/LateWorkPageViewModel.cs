using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class LateWorkPageViewModel :
    ObservableObject,
    IBusyViewModel
{
    private readonly TeacherAssessmentService _service;
    private readonly RegistrarService _registrar;

    [ObservableProperty] ObservableCollection<SectionLateWorkCollection> sections = [];
    [ObservableProperty] SectionLateWorkCollection selectedSection = new();
    [ObservableProperty] bool showSelectedSection;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public LateWorkPageViewModel(TeacherAssessmentService service, RegistrarService registrar)
    {
        _service = service;
        _registrar = registrar;
    }

    public async Task Initialize(ErrorAction onError)
    {
        await _service.WaitForInit(onError);
        await _registrar.WaitForInit(onError);

        var myClasses = _registrar.MyAcademicSchedule;
        Sections = [.. myClasses.Select(sec => new SectionLateWorkCollection(SectionViewModel.Get(sec)))];
        foreach(var section in Sections)
        {
            section.Selected += (_, _) =>
            {
                ShowSelectedSection = section.IsSelected;
                foreach(var sec in Sections)
                {
                    sec.IsSelected = sec.Section.Model.Reduce(SectionRecord.Empty).sectionId == section.Section.Model.Reduce(SectionRecord.Empty).sectionId && section.IsSelected;
                }
                SelectedSection = section.IsSelected ? section : new();
            };

            section.PropertyChanged += ((IBusyViewModel)(this)).BusyChangedCascade;
        }
    }
}

public partial class LateWorkViewModel :
    ObservableObject,
    IModelCarrier<LateWorkViewModel, LateWorkRecord>,
    IErrorHandling
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();

    public OptionalStruct<LateWorkRecord> Model { get; private set; } = OptionalStruct<LateWorkRecord>.None();

    [ObservableProperty] string id = "";
    [ObservableProperty] string details = "";
    [ObservableProperty] DateTime resolved;
    [ObservableProperty] bool isResolved;
    [ObservableProperty] DateTime marked;
    [ObservableProperty] bool isAssessment;

    public event EventHandler<ErrorRecord>? OnError;

    public static LateWorkViewModel Get(LateWorkRecord model)
    {
        var vm = new LateWorkViewModel()
        {
            Model = OptionalStruct<LateWorkRecord>.Some(model),
            Id = model.id,
            Details = model.details,
            Marked = model.markedDate.ToDateTime(default),
            IsResolved = model.isResolved,
            Resolved = model.resolvedDate ?? default,
            IsAssessment = model.isAssessment
        };

        return vm;
    }

    [RelayCommand]
    public async Task Resolve()
    {
        if (IsAssessment)
            await _service.ResolveLateAssessment(this.Id, OnError.DefaultBehavior(this));
        else
            await _service.ResolveLateWorkPattern(this.Id, OnError.DefaultBehavior(this));
    }
}

public partial class StudentLateWorkCollectionViewModel : 
    ObservableObject,
    IErrorHandling
{
    [ObservableProperty] StudentViewModel student = new();
    [ObservableProperty] int totalLateWork;
    [ObservableProperty] int outstandingLateWork;
    [ObservableProperty] ObservableCollection<LateWorkViewModel> lateWorkPatterns = [];
    [ObservableProperty] ObservableCollection<LateWorkViewModel> lateAssessments = [];

    public StudentLateWorkCollectionViewModel(StudentLateWorkCollection lateWork)
    {
        lateWork.Split( out var assessments, out var patterns);
        Student = StudentViewModel.Get(lateWork.student);
        LateWorkPatterns = [.. patterns.lateWork.Select(LateWorkViewModel.Get).OrderBy(lw => lw.Marked)];
        LateAssessments = [.. assessments.lateWork.Select(LateWorkViewModel.Get).OrderBy(lw => lw.Marked)];
        TotalLateWork = lateWork.lateWork.Length;
        OutstandingLateWork = lateWork.lateWork.Count(lw => !lw.isResolved);

        foreach (var lw in LateWorkPatterns.Union(LateAssessments))
            lw.OnError += (sender, e) => OnError?.Invoke(sender, e);
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

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<SectionLateWorkCollection>? Selected;

    [ObservableProperty] SectionViewModel section = SectionViewModel.Empty;
    [ObservableProperty] ObservableCollection<StudentLateWorkCollectionViewModel> lateWorkByStudent = [];
    [ObservableProperty] bool showResolved;
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] ObservableCollection<AssessmentDetailsViewModel> assessments;
    [ObservableProperty] bool showAssessments;
    [ObservableProperty] bool assessmentSelected;
    [ObservableProperty] 

    public SectionLateWorkCollection() { }

    public SectionLateWorkCollection(SectionViewModel section)
    {
        Section = section;
        LoadLateWork().SafeFireAndForget(e => e.LogException());
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

public partial class CreateLateAssessmentViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    ISelectable<CreateLateAssessmentViewModel>
{
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<CreateLateAssessmentViewModel>? Selected;

    [ObservableProperty] AssessmentEditorViewModel assessment;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool isSelected;

    [ObservableProperty] string details = "";
    [ObservableProperty] ObservableCollection<StudentViewModel> students = [];

    public static implicit operator CreateLateAssessmentViewModel(AssessmentEditorViewModel assessment) => new CreateLateAssessmentViewModel(assessment);

    public CreateLateAssessmentViewModel(AssessmentEditorViewModel assessment)
    {
        this.assessment = assessment;

    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

