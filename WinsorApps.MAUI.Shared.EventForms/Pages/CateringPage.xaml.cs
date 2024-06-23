using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class CateringPage : ContentPage
{

	public CateringPage(CateringEventViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
        vm.ReadyToContinue += Vm_ReadyToContinue;
		BindingContext = vm;
		InitializeComponent();
	}

    private void Vm_ReadyToContinue(object? sender, EventArgs e)
    {

    }
}