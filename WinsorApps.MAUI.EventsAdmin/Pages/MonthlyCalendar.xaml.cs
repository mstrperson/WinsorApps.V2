using WinsorApps.MAUI.EventsAdmin.ViewModels;

namespace WinsorApps.MAUI.EventsAdmin.Pages;

public partial class MonthlyCalendar : ContentPage
{
	AdminCalendarViewModel ViewModel => (AdminCalendarViewModel)BindingContext;
	public MonthlyCalendar(AdminCalendarViewModel vm)
	{
		BindingContext = vm;
		InitializeComponent();
	}

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
		ViewModel.Refresh();
    }
}