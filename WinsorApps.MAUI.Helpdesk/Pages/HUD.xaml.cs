using WinsorApps.MAUI.Helpdesk.Pages.ServiceCase;
using WinsorApps.MAUI.Helpdesk.ViewModels;

namespace WinsorApps.MAUI.Helpdesk.Pages;

public partial class HUD : ContentPage
{
	public HUD(HudViewModel vm)
	{
		BindingContext = vm;
		vm.OnError += this.DefaultOnErrorHandler();
        vm.OnCaseSelected += Vm_OnCaseSelected;
		vm.PageRequested += async (_, page) => await Navigation.PushAsync(page);
		vm.PopStackRequested += async (_, _) => await Navigation.PopToRootAsync(true);
        
		InitializeComponent();
	}

    private void Vm_OnCaseSelected(object? sender, ViewModels.ServiceCases.ServiceCaseViewModel e)
    {
		var page = new ServiceCaseEditor(e);
		Navigation.PushAsync(page);
    }
}