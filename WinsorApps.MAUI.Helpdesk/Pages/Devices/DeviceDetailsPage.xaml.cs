using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.Helpdesk.Pages.Devices;

public partial class DeviceDetailsPage : ContentPage
{
	public DeviceViewModel ViewModel
	{
		get => (DeviceViewModel)BindingContext;
		set
		{
			BindingContext = value;
            ViewModel.NewServiceCaseRequested += ViewModel_NewServiceCaseRequested;
            ViewModel.ServiceCaseSelected += ViewModel_ServiceCaseSelected;
			ViewModel.OnError += this.DefaultOnErrorHandler();
		}
	}

    private void ViewModel_ServiceCaseSelected(object? sender, ViewModels.ServiceCases.ServiceCaseViewModel e)
    {
        // TODO: Load ServiceCasePage with this ViewModel.
    }

    private void ViewModel_NewServiceCaseRequested(object? sender, DeviceViewModel e)
    {
		// TODO: Instantiate new ServiceCase page.
    }

    public DeviceDetailsPage()
	{
		InitializeComponent();
	}
}