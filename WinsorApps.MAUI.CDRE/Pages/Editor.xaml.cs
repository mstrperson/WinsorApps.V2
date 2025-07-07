using WinsorApps.MAUI.CDRE.ViewModels;

namespace WinsorApps.MAUI.CDRE.Pages;

public partial class Editor : ContentPage
{
	private RecurringEventViewModel ViewModel => (RecurringEventViewModel)BindingContext; 

	public Editor(RecurringEventViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
        vm.PropertyChanged += Vm_PropertyChanged;
	}

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if(e.PropertyName == "Busy" && ViewModel.Busy)
		{
			Loading.Focus();
		}

		if(e.PropertyName == "StartTime")
		{
			ViewModel.EndTime = ViewModel.StartTime.Add(TimeSpan.FromMinutes(30));
		}
    }
}