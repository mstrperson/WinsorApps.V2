using WinsorApps.MAUI.Helpdesk.ViewModels;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.Helpdesk.Pages;

public partial class HUD : ContentPage
{
	public HUD(HudViewModel vm)
	{
		BindingContext = vm;
		vm.OnError += this.DefaultOnErrorHandler();
		InitializeComponent();
	}
}