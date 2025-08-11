using WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.StudentAssessmentCalendar.Pages;

public partial class AssessmentPage : ContentPage
{
	public AssessmentPage(StudentAssessmentViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		vm.LatePassRequested += (_, _) =>
		{
			var page = new LatePassRequest(vm);
			Navigation.PushAsync(page);
		};

		vm.LatePassSubmitted += (_, _) =>
		{
			Navigation.PopAsync();
		};

		BindingContext = vm;
		InitializeComponent();
	}
}