using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.BookstoreManager.ViewModels;

public partial class CreateTeacherOrderViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherBookstoreService _service;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<SectionViewModel>? SectionCreated;

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    [ObservableProperty] private UserSearchViewModel teacherSearch = new();
    [ObservableProperty] private UserViewModel selectedTeacher = UserViewModel.Empty;
    [ObservableProperty] private DepartmentSearchViewModel departmentList = new(ServiceHelper.GetService<RegistrarService>());
    [ObservableProperty] private DepartmentViewModel selectedDepartment = DepartmentViewModel.Empty;
    [ObservableProperty] private CourseViewModel selectedCourse = CourseViewModel.Empty;
    [ObservableProperty] private bool fallOrFullYear = true;


    public CreateTeacherOrderViewModel(TeacherBookstoreService service)
    {
        _service = service;
        teacherSearch.OnSingleResult += (sender, e) =>
        {
            SelectedTeacher = e;
        };
        teacherSearch.OnError += (sender, e) => OnError?.Invoke(sender, e);
        departmentList.OnError += (sender, e) => OnError?.Invoke(sender, e);
        foreach(var dept in departmentList.Departments)
        {
            dept.OnError += (sender, e) => OnError?.Invoke(sender, e);
            dept.Selected += (_, _) => SelectedDepartment = dept;
            foreach(var course in dept.Courses)
            {
                course.Selected += (_, _) => SelectedCourse = course;
            }
        }
    }

    [RelayCommand]
    public void Reset()
    {
        TeacherSearch.ClearSelection();
    }

    [RelayCommand]
    public void ToggleFallOrFullYear()
    {
        FallOrFullYear = !FallOrFullYear;
    }

    [RelayCommand]
    public async Task StartBookOrder()
    {
        var result = await _service.CreateNewSection(SelectedCourse.Id, FallOrFullYear, OnError.DefaultBehavior(this));
        if (result is null)
            return;

        var vm = SectionViewModel.Get(result);
        SectionCreated?.Invoke(this, vm);
    }
}


