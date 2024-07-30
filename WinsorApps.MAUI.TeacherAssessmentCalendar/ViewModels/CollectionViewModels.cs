using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class MyStudentsViewModel :
    ObservableObject
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();
    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty] ObservableCollection<UserViewModel> students = [];

    public async Task Initialize(ErrorAction onError)
    {
        await _service.WaitForInit(onError);

        Students = [.._service.MyStudents.Select(stu => UserViewModel.Get(_registrar.StudentList.First(u => u.id == stu.id)))];
    }
}

public partial class MyClassesViewModel :
    ObservableObject
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();
    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty] ObservableCollection<SectionViewModel> fallSemester = [];
    [ObservableProperty] ObservableCollection<SectionViewModel> springSemester = [];

    public async Task Intitalize(ErrorAction onError)
    {
        await _registrar.WaitForInit(onError);

        var myClasses = SectionViewModel.GetClonedViewModels(_registrar.MyAcademicSchedule);
        FallSemester = [.. myClasses.Where(sec => sec.Term.StartsWith("first", StringComparison.InvariantCultureIgnoreCase))];
        SpringSemester = [.. myClasses.Except(FallSemester)];
    }
}
