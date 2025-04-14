using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;

namespace WinsorApps.MAUI.Helpdesk.Pages.Devices;

public partial class DeviceEditor : ContentPage
{
	public DeviceEditor(DeviceViewModel viewModel)
	{
		BindingContext = viewModel;
		InitializeComponent();
	}

	public DeviceEditor()
	{
		InitializeComponent();
	}
}