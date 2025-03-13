using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;
using WinsorApps.Services.AssessmentCalendar.Services;
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

    public event EventHandler<ContentPage>? PageRequested;

    public event EventHandler<AssessmentEditorViewModel>? SectionAssessmentSelected;
    public event EventHandler<AssessmentDetailsViewModel>? DetailsRequested;
    public event EventHandler<AssessmentDetailsViewModel>? ShowDetailsPageRequested;
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

        AssessmentCollection.ShowDetailsPageRequested += (sender, details) => ShowDetailsPageRequested?.Invoke(sender, details);

        AssessmentCollection.StudentSelected += async (sender, e) =>
        {
            var studentPage = ServiceHelper.GetService<StudentPage>();
            studentPage.ViewModel.SearchText = e.Student.UserInfo.DisplayName;
            await studentPage.ViewModel.SearchStudents();
            PageRequested?.Invoke(this, studentPage);
        };


        _service = service;
        _registrar = registrar;

        _service.OnCacheRefreshed += (_, _) => 
            Refresh().SafeFireAndForget(e => e.LogException());

        Refresh().SafeFireAndForget(e => e.LogException());
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

        var myCourseIds = _registrar.MyAcademicSchedule
            .Select(sec => sec.courseId).Distinct();
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
            await course.LoadSections();
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
        await _registrar.WaitForInit(OnError.DefaultBehavior(this));
        await _registrar.GetMyAcademicScheduleAsync(true);
        await Initialize(OnError.DefaultBehavior(this));
        await AssessmentCollection.Refresh();
        Busy = false;
    }
}
