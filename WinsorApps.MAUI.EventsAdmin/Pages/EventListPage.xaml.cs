using AsyncAwaitBestPractices;
using WinsorApps.MAUI.EventsAdmin.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.Pages;
using WinsorApps.Services.Global;

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
		};
	}

    private void Vm_FormSelected(object? sender, AdminFormViewModel e)
    {
		var page = new FormEditor(e.Form);
		Navigation.PushAsync(page);
    }

    private void EventListPage_OnNavigatedTo(object? sender, EventArgs e)
    {
	    
    }

    private void VisualElement_OnLoaded(object? sender, EventArgs e)
    {
	    if (ViewModel is null || ViewModel.HasLoaded) 
		    return;
			
	    ViewModel.Busy = true;
	    ViewModel.BusyMessage = "Loading..."; 
	    ViewModel.Initialize(this.DefaultOnErrorAction()).SafeFireAndForget(ex => 
		    ex.LogException());
    }

}