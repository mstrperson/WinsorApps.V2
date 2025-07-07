using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class ScheduleViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<SectionAssessmentCalendarViewModel>? SectionSelected;

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    [ObservableProperty] private ObservableCollection<SectionAssessmentCalendarViewModel> sections = [];

    [ObservableProperty] private AssessmentDetailsViewModel selectedAssessment = new();
    [ObservableProperty] private bool showAssessment;

    [ObservableProperty] private bool loaded;
    [ObservableProperty] private UserViewModel student = new();
    public async Task GetScheduleFor(UserViewModel student)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        var logging = ServiceHelper.GetService<LocalLoggingService>();
        var sections = (await registrar
            .GetAcademicScheduleFor(student.Model.Reduce(UserRecord.Empty).id, OnError.DefaultBehavior(this)))
            .Where(section => section.isCurrent);
        Student = student;
        Sections = [.. SectionViewModel.GetClonedViewModels(sections)];
        foreach (var section in Sections)
        {
            section.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            section.OnError += (sender, e) => OnError?.Invoke(sender, e);
            section.Selected += (_, _) =>
            {
                SelectedAssessment = new();
                ShowAssessment = false;
                SectionSelected?.Invoke(this, section);
            };
            section.AssessmentSelected += (_, assessment) =>
            {
                SelectedAssessment = assessment;
                SelectedAssessment.SelectStudent(Student.Id);
                ShowAssessment = true;
            };
        }

        Loaded = true;
    }
}

public partial class SectionAssessmentCalendarViewModel :
    ObservableObject,
    IBusyViewModel,
    IErrorHandling,
    ISelectable<SectionAssessmentCalendarViewModel>
{
    private readonly ReadonlyCalendarService _calendar = ServiceHelper.GetService<ReadonlyCalendarService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<SectionAssessmentCalendarViewModel>? Selected;
    public event EventHandler<AssessmentDetailsViewModel>? AssessmentSelected;

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    [ObservableProperty] private bool isSelected;

    [ObservableProperty] private UserViewModel student = UserViewModel.Empty;

    [ObservableProperty] private SectionViewModel section = SectionViewModel.Empty;
    [ObservableProperty] private ObservableCollection<AssessmentDetailsViewModel> assessments = [];
    [ObservableProperty] private ObservableCollection<AssessmentDetailsViewModel> displayedAssessments = [];
    [ObservableProperty] private bool hasAssessments;
    [ObservableProperty] private bool showPastAssessments;

    public static implicit operator SectionAssessmentCalendarViewModel(SectionViewModel section) => new(section);

    public SectionAssessmentCalendarViewModel(SectionViewModel section)
    {
        Section = section;
    }

    [RelayCommand]
    public async Task LoadAssessments()
    {
        var result = await _calendar.GetAssessmentsFor(Section.Model.Reduce(SectionRecord.Empty).sectionId, OnError.DefaultBehavior(this));
        Assessments = [.. result.Select(AssessmentDetailsViewModel.Get)];
        foreach(var assessment in Assessments)
        {
            assessment.Selected += (_, _) => AssessmentSelected?.Invoke(this, assessment);
        }
        DisplayedAssessments = ShowPastAssessments ? Assessments : 
            [.. Assessments.Where(assessment => assessment.Model.Reduce(AssessmentCalendarEvent.Empty).start >= DateTime.Today)];
        HasAssessments = DisplayedAssessments.Count > 0;
    }

    [RelayCommand]
    public void ToggleShowPastAssessments()
    {
        ShowPastAssessments = !ShowPastAssessments;
        DisplayedAssessments = ShowPastAssessments ? Assessments : 
            [.. Assessments.Where(assessment => assessment.Model.Reduce(AssessmentCalendarEvent.Empty).start >= DateTime.Today)];
        HasAssessments = DisplayedAssessments.Count > 0;
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}
