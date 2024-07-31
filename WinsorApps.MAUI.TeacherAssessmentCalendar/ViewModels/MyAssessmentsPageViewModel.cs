using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class MyAssessmentsPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherAssessmentService _service;
    private readonly RegistrarService _registrar;

    [ObservableProperty] MyAssessmentsCollectionViewModel assessmentCollection;

    [ObservableProperty] AssessmentDetailsViewModel selectedDetails = new();
    [ObservableProperty] bool showDetails;

    [ObservableProperty] ObservableCollection<CourseViewModel> myCourses = [];
    [ObservableProperty] CourseViewModel selectedCourse = CourseViewModel.Empty;
    [ObservableProperty] bool courseSelected;
    [ObservableProperty] bool showCourseSelection;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<AssessmentEditorViewModel>? SectionAssessmentSelected;
    public event EventHandler<ErrorRecord>? OnError;

    public MyAssessmentsPageViewModel(MyAssessmentsCollectionViewModel assessmentCollection, TeacherAssessmentService service, RegistrarService registrar)
    {
        AssessmentCollection = assessmentCollection;
        AssessmentCollection.OnError += (sender, e) => OnError?.Invoke(sender, e);
        AssessmentCollection.ShowDetailsRequested += (sender, details) =>
        {
            ShowDetails = true;
            SelectedDetails = details;
        };

        AssessmentCollection.StudentSelected += (sender, e) =>
        {

        };

        _service = service;
        _registrar = registrar;
    }

    [RelayCommand]
    public void HideDetails()
    {
        ShowDetails = false;
    }

    [RelayCommand]
    public void ToggleShowCourseSelection()
    {
        ShowCourseSelection = !ShowCourseSelection;
    }

    public async Task Initialize(ErrorAction onError)
    {
        await _registrar.WaitForInit(onError);

        var myCourseIds = _registrar.MyAcademicSchedule.Select(sec => sec.courseId).Distinct();
        var courses = _registrar.CourseList.Where(course => myCourseIds.Contains(course.courseId));
        MyCourses = [.. CourseViewModel.GetClonedViewModels(courses)];
        foreach(var course in MyCourses)
        {
            course.Selected += (_, _) =>
            {
                CourseSelected = course.IsSelected;
                ShowCourseSelection = false;
                SelectedCourse = CourseSelected ? course : CourseViewModel.Empty;
            };
        }
    }

    [RelayCommand]
    public async Task StartNewAssessment()
    {
        if (!CourseSelected || !SelectedCourse.IsSelected)
            return;

        await AssessmentCollection.AddGroupFor(SelectedCourse);

        CourseSelected = false;
        SelectedCourse.IsSelected = false;
        SelectedCourse = CourseViewModel.Empty;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Reloading my assessments.";
        await Initialize(OnError.DefaultBehavior(this));
        await AssessmentCollection.Refresh();
        Busy = false;
    }
}
