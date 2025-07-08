using AsyncAwaitBestPractices;
using WinsorApps.MAUI.EventsAdmin.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.Pages;

namespace WinsorApps.MAUI.EventsAdmin.Pages;

public partial class EventListPage : ContentPage
{
	public EventListPageViewModel? ViewModel => BindingContext as EventListPageViewModel;
	public EventListPage(EventListPageViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
        vm.FormSelected += Vm_FormSelected;
		BindingContext = vm;


		InitializeComponent();
		this.NavigatedTo += (_, _) =>
		{
			if (vm.HasLoaded) return;
			
			vm.Busy = true;
			vm.BusyMessage = "Loading..."; 
			vm.Initialize(this.DefaultOnErrorAction()).SafeFireAndForget(e => e.LogException());
		};
	}

    private void Vm_FormSelected(object? sender, AdminFormViewModel e)
    {
		var page = new FormEditor(e.Form);
		Navigation.PushAsync(page);
    }
}