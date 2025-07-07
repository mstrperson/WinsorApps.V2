using AsyncAwaitBestPractices;
using WinsorApps.MAUI.EventForms.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.Pages;

namespace WinsorApps.MAUI.EventForms.Pages;

public partial class MonthlyCalendar : ContentPage
{
	private EventsCalendarViewModel ViewModel => (EventsCalendarViewModel)BindingContext;
	public MonthlyCalendar(EventsCalendarViewModel vm)
	{
		BindingContext = vm;
        vm.LoadEvent += Vm_LoadEvent;
		InitializeComponent();
    }

    private void Vm_LoadEvent(object? sender, Shared.EventForms.ViewModels.EventFormViewModel e)
    {
        var page = new FormEditor(e);
        Navigation.PushAsync(page);
    }

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        ViewModel.Refresh().SafeFireAndForget(e => e.LogException());
    }
}