using WinsorApps.MAUI.EventsAdmin.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.Pages;

namespace WinsorApps.MAUI.EventsAdmin.Pages;

public partial class EventListPage : ContentPage
{
	public EventListPage(EventListPageViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
        vm.FormSelected += Vm_FormSelected;
		BindingContext = vm;
		InitializeComponent();
	}

    private void Vm_FormSelected(object? sender, AdminFormViewModel e)
    {
		var page = new FormEditor(e.Form);
		Navigation.PushAsync(page);
    }
}