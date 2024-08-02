using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class StudentPage : ContentPage
{
	public StudentPage(StudentPageViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		BindingContext = vm;

		InitializeComponent();
	}
}