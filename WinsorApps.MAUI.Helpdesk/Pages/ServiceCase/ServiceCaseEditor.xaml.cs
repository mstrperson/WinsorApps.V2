using System.Diagnostics;
using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;

namespace WinsorApps.MAUI.Helpdesk.Pages.ServiceCase;

public partial class ServiceCaseEditor : ContentPage
{
	public ServiceCaseViewModel ViewModel => (ServiceCaseViewModel)BindingContext;

	public ServiceCaseEditor(ServiceCaseViewModel vm)
	{
		BindingContext = vm;
		ViewModel.OnError += this.DefaultOnErrorHandler();
        ViewModel.OnCreate += Rebind;
		ViewModel.OnUpdate += Rebind;
		ViewModel.OnClose += Rebind;
		InitializeComponent();

		LoanerSearch.ViewModel.OnSingleResult += (sender, dev) =>
		{
			ViewModel.SetLoaner(dev);
		};
		LoanerSearch.ViewModel.OnZeroResults += (sender, e) =>
		{
			ViewModel.SetLoaner(IEmptyViewModel<DeviceViewModel>.Empty);
		};

        if (!string.IsNullOrEmpty(ViewModel.Loaner.SerialNumber))
            LoanerSearch.Select(ViewModel.Loaner);

        Debug.WriteLine($"{vm.Loaner.DisplayName}");
	}

    private void Rebind(object? sender, ServiceCaseViewModel e)
    {
		BindingContext = e;
    }
}