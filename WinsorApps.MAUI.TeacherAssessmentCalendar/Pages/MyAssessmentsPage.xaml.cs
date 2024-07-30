using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class MyAssessmentsPage : ContentPage
{
	public MyAssessmentsPageViewModel ViewModel => (MyAssessmentsPageViewModel)BindingContext;

	public MyAssessmentsPage(MyAssessmentsPageViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
        vm.StudentSelected += Vm_StudentSelected;

		BindingContext = vm;
		InitializeComponent();
	}

    private void Vm_StudentSelected(object? sender, UserViewModel e)
    {

    }

    private void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        foreach (var c in ViewModel.MyCourses)
        {
            c.IsSelected = false;
        }

        if (sender is Picker picker && picker.SelectedItem is CourseViewModel course)
		{
			course.Select();
		}
    }
}