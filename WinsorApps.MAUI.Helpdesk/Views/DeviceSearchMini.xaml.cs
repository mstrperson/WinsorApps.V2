using System.Collections.Immutable;
using System.Diagnostics;
using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.Views;

public partial class DeviceSearchMini : ContentView
{
	public static BindableProperty SelectedDeviceProperty =
		BindableProperty.Create(nameof(SelectedDevice), typeof(DeviceViewModel), typeof(DeviceSearchMini), IEmptyViewModel<DeviceViewModel>.Empty, BindingMode.TwoWay);

	public static BindableProperty LoanerSearchProperty =
		BindableProperty.Create(nameof(LoanerSearch), typeof(bool), typeof(DeviceSearchMini), false, BindingMode.TwoWay);
		 
	public bool LoanerSearch
	{
		get => (bool)GetValue(LoanerSearchProperty);
		set
		{
			SetValue(LoanerSearchProperty, value);
			if(value)
			{
				var deviceService = ServiceHelper.GetService<DeviceService>();
				ViewModel.Available = DeviceViewModel.GetClonedViewModels(deviceService.Loaners).ToImmutableArray();
			}
		}
	}

	public DeviceViewModel SelectedDevice
	{
		get
        {
            var vm = (DeviceViewModel)GetValue(SelectedDeviceProperty);
			if (ViewModel.Selected.Id != vm.Id)
				Debug.WriteLine("wtf...");
			return vm;
		}
		set
		{
			if(ViewModel.Selected.Id != value.Id)
				ViewModel.Select(value);
			SetValue(SelectedDeviceProperty, value);
		}
	}

	public DeviceSearchViewModel ViewModel => (DeviceSearchViewModel)BindingContext;
	public DeviceSearchMini()
	{
		InitializeComponent();
		BindingContext = new DeviceSearchViewModel();
		ViewModel.OnSingleResult += (_, dev) => SelectedDevice = dev;
		Debug.WriteLine($"{SelectedDevice.DisplayName}");
	}

	public void Select(DeviceViewModel dev)
	{
		ViewModel.Select(dev);
        Debug.WriteLine($"{SelectedDevice.DisplayName}");
    }
}