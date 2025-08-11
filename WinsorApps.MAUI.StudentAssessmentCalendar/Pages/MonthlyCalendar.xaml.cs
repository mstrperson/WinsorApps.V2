using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.StudentAssessmentCalendar.Pages;

public partial class MonthlyCalendar : ContentPage
{
	public MonthlyCalendar(MonthlyViewModel vm)
	{
		BindingContext = vm;
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