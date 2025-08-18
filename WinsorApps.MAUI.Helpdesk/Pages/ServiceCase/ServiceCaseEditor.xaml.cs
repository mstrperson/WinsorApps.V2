using System.Diagnostics;
using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;
using WinsorApps.MAUI.Shared;

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
		//ViewModel.OnClose += Rebind;
		ViewModel.RequiredConfirmationRequested += async (_, vm) =>
		{
			vm.WaitingForFMM = 
				await DisplayAlert("Required Confirmation", $"Does {vm.Device.Owner.FirstName} still need to disable Find My Mac?", "Yes", "No");
			
			if (!vm.WaitingForFMM)
				vm.DisabledFMM = await DisplayAlert("Required Confirmation", "Was Find My Mac Disabled? (Say `Yes` if no iCloud account is signed in)", "Yes", "No");

			vm.BackupNeeded = await DisplayAlert("Required Confirmation", "Is a backup needed before we send it out?", "Yes", "No");

            vm.RequiredConfirmation = true;
			await vm.Submit();
		};

        InitializeComponent();

		LoanerSearch.ViewModel.OnSingleResult += (sender, dev) =>
		{
			ViewModel.SetLoaner(dev);
		};
		LoanerSearch.ViewModel.OnZeroResults += (sender, e) =>
		{
			ViewModel.SetLoaner(DeviceViewModel.Empty);
		};

        if (!string.IsNullOrEmpty(ViewModel.Loaner.SerialNumber))
            LoanerSearch.Select(ViewModel.Loaner);

        Debug.WriteLine($"{vm.Loaner.DisplayName}");
	}

    private void Rebind(object? sender, ServiceCaseViewModel e)
    {
		BindingContext = e;
		if (!e.Status.Closed)
			MainThread.BeginInvokeOnMainThread(async () => await e.PrintSticker());
    }
}