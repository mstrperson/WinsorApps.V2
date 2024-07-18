using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class CateringPage : ContentPage
{

	public CateringPage(CateringEventViewModel vm)
    {
        vm.OnError += this.DefaultOnErrorHandler();
        vm.Deleted += async (_, _) => await Navigation.PopAsync();
        vm.ReadyToContinue += async (_, _) => await Navigation.PopAsync();
        BindingContext = vm;
		InitializeComponent();
	}
}