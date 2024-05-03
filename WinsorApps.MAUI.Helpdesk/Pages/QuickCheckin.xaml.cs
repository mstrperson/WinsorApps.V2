using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.Helpdesk.Pages;

public partial class QuickCheckin : ContentPage
{
	public QuickCheckin(QuickCheckinViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		this.BindingContext = vm;
		InitializeComponent();
	}
}