using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;
using WinsorApps.Services.Global;

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
	}

	private void Vm_EventSelected(object? sender, AssessmentCalendarEventViewModel e)
	{
		var page = new AssessmentPage(e);
		Navigation.PushAsync(page);
	}
}