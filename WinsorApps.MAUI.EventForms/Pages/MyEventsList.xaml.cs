using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.Pages;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.EventForms.Pages;

public partial class MyEventsList : ContentPage
{
	public MyEventsList()
	{
		var vm = new EventListViewModel() { Start = new(DateTime.Today.Year, DateTime.Today.Month, 1), End = (new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).AddMonths(1), PageLabel=$"{DateTime.Today:MMMM yyyy}" };
		vm.OnError += this.DefaultOnErrorHandler();
        vm.OnEventSelected += EventSelected;
		vm.Reload().SafeFireAndForget(e => e.LogException());
		BindingContext = vm;
		InitializeComponent();
	}

    private void EventSelected(object? sender, EventFormViewModel e)
    {
		var editor = new FormEditor(e);
		Navigation.PushAsync(editor);
    }
}