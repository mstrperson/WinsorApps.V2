using System.Diagnostics;
using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Shared.ViewModels;

namespace WinsorApps.MAUI.Helpdesk.Views;

public partial class DeviceSearchMini : ContentView
{
	public static BindableProperty SelectedDeviceProperty =
		BindableProperty.Create(nameof(SelectedDevice), typeof(DeviceViewModel), typeof(DeviceSearchMini), IEmptyViewModel<DeviceViewModel>.Empty, BindingMode.TwoWay);

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
	}
}