using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.StudentAssessmentCalendar.Pages;

public partial class AssessmentPage : ContentPage
{
	public AssessmentPage(StudentAssessmentViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		BindingContext = vm;
		InitializeComponent();
	}
}