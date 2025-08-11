using WinsorApps.MAUI.Helpdesk.ViewModels;

namespace WinsorApps.MAUI.Helpdesk.Pages;

public partial class DeviceCollectionPage : ContentPage
{
	public DeviceCollectionPage(DeviceCollectionPageViewModel vm)
	{
		this.BindingContext = vm;
		vm.OnError += this.DefaultOnErrorHandler();
        InitializeComponent();
	}
}