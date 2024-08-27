using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class MonthlyCalendar : ContentPage
{
	public MonthlyCalendar(MonthlyCalendarViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		BindingContext = vm;
		InitializeComponent();
	}
}