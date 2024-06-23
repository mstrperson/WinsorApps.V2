using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;

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
        ViewModel.OnZeroResults += ViewModel_OnZeroResults;
		ViewModel.OnError += this.DefaultOnErrorHandler();
		InitializeComponent();
	}

    private void ViewModel_OnZeroResults(object? sender, EventArgs e)
    {
		var vm = DeviceViewModel.Default;
		vm.SerialNumber = ViewModel.SearchText;
		vm.OnError += this.DefaultOnErrorHandler();
		vm.Selected += ViewModel_OnSingleResult;
        DeviceEditor page = new() { BindingContext = vm };
		Navigation.PushAsync(page);
    }

    private void ViewModel_OnSingleResult(object? sender, DeviceViewModel e)
    {
		DeviceDetailsPage page = new() { ViewModel = e };
		Navigation.PushAsync(page);
    }
}