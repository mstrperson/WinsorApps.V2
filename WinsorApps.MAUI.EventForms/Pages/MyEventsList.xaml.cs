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
        vm.PageRequested += Vm_PageRequested;
        vm.PopThenPushRequested += async (sender, e) =>
        {
            await Navigation.PopToRootAsync();
            Vm_PageRequested(sender, e);
        };
		vm.Reload().SafeFireAndForget(e => e.LogException());
		BindingContext = vm;
		InitializeComponent();
	}


    private void Vm_PageRequested(object? sender, ContentPage e)
    {
        Navigation.PushAsync(e);
    }

    private void EventSelected(object? sender, EventFormViewModel vm)
    {
        var editor = new FormEditor(vm);
        Vm_PageRequested(sender, editor);
    }
}