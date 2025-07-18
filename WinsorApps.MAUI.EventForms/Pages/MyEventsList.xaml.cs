using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.Pages;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.Services.Global;

namespace WinsorApps.MAUI.EventForms.Pages;

public partial class MyEventsList : ContentPage
{
	public EventListViewModel ViewModel => (EventListViewModel) BindingContext;
	public MyEventsList(EventListViewModel vm)
	{
		vm.Start = DateTime.Today.MonthOf();
        vm.End = vm.Start.AddMonths(1);
        vm.PageLabel=$"{DateTime.Today:MMMM yyyy}";
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

    private void MyEventsList_OnNavigatedTo(object? sender, NavigatedToEventArgs e)
    {
	    ViewModel.Reload().SafeFireAndForget(x=> x.LogException());
    }

    private void DatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        ViewModel.Start = ViewModel.Start.MonthOf();
        ViewModel.End = ViewModel.Start.AddMonths(1);
        ViewModel.ShowDatePicker = false;
        ViewModel.Reload().SafeFireAndForget(x => x.LogException());
    }
}