using WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.StudentAssessmentCalendar.Pages;

public partial class WeeklyCalendar : ContentPage
{
	public WeeklyCalendar(WeeklyViewModel vm)
	{
		BindingContext = vm;
		vm.Calendar.Week.Monday = DateTime.Today.MondayOf();
		vm.OnError += this.DefaultOnErrorHandler();
		vm.EventSelected += Vm_EventSelected;
		InitializeComponent();
		vm.Initialize(this.DefaultOnErrorAction()).SafeFireAndForget(e => e.LogException());
	}

	private void Vm_EventSelected(object? sender, StudentAssessmentViewModel e)
	{
		var page = new AssessmentPage(e);
		Navigation.PushAsync(page);
	}
}