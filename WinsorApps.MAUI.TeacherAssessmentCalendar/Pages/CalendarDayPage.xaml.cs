using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class CalendarDayPage : ContentPage
{
	public CalendarDayPage(CalendarDayViewModel vm)
	{
		BindingContext = vm;
		InitializeComponent();
	}
}