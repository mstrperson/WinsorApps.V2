using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class MonthlyCalendar : ContentPage
{
	public MonthlyCalendar(MonthlyCalendarViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		BindingContext = vm;
        vm.DaySelected += Vm_DaySelected;
		InitializeComponent();
	}

    private void Vm_DaySelected(object? sender, Shared.AssessmentCalendar.ViewModels.CalendarDayViewModel e)
    {
		var page = new CalendarDayPage(e);
		Navigation.PushAsync(page);
    }
}