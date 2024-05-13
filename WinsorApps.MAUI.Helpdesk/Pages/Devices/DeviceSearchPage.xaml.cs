using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.Helpdesk.Pages.Devices;

public partial class DeviceSearchPage : ContentPage
{
	public DeviceSearchViewModel ViewModel
	{ 
		get => (DeviceSearchViewModel)BindingContext;
		private set => BindingContext = value;
	}

	public DeviceSearchPage()
	{
		ViewModel = new();
        ViewModel.OnSingleResult += ViewModel_OnSingleResult;
		ViewModel.OnError += this.DefaultOnErrorHandler();
		InitializeComponent();
	}

    private void ViewModel_OnSingleResult(object? sender, DeviceViewModel e)
    {
		DeviceDetailsPage page = new() { ViewModel = e };
		Navigation.PushAsync(page);
    }
}