using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class FacilitesPage : ContentPage
{
	public FacilitesPage(FacilitesEventViewModel vm)
    {
        vm.OnError += this.DefaultOnErrorHandler();
        vm.Deleted += async (_, _) => await Navigation.PopAsync();
        vm.ReadyToContinue += async (_, _) => await Navigation.PopAsync();
        BindingContext = vm;
        InitializeComponent();
	}
}