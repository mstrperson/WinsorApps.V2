using WinsorApps.MAUI.Helpdesk.Pages.ServiceCase;
using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;
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
		e.OnClose += async (_, _) =>
		{
			await Navigation.PopToRootAsync();
		};

		e.OnUpdate += async (_, _) =>
		{
			await Navigation.PopToRootAsync();
		};

		e.OnError += this.DefaultOnErrorHandler();

		ServiceCaseEditor page = new(e);
		page.ViewModel.OnError += this.DefaultOnErrorHandler();
		page.ViewModel.OnUpdate += async (_, sc) =>
		{
			var exiting = ViewModel.ServiceHistory.FirstOrDefault(c => c.Id == sc.Id);
			if (exiting is null)
				ViewModel.ServiceHistory = [.. ViewModel.ServiceHistory, sc];
			else
				ViewModel.ServiceHistory = ViewModel.ServiceHistory.Replace(exiting, sc);
            await Navigation.PopAsync();
        };
		Navigation.PushAsync(page);
    }

    private void ViewModel_NewServiceCaseRequested(object? sender, DeviceViewModel e)
    {
		var vm = new ServiceCaseViewModel() { Device = e };
		vm.OnCreate += async (_, sc) =>
		{
			ViewModel.ServiceHistory = [.. ViewModel.ServiceHistory, sc];
			await Navigation.PopAsync();
		};

		ViewModel_ServiceCaseSelected(sender, vm);
    }

    public DeviceDetailsPage()
	{
		InitializeComponent();
	}
}