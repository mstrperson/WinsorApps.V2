using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class MarComPage : ContentPage
{
	public MarComPage(MarCommEventViewModel vm)
    {
        vm.OnError += this.DefaultOnErrorHandler();
        vm.Deleted += async (_, _) => await Navigation.PopAsync();
        vm.ReadyToContinue += async (_, _) => await Navigation.PopAsync();
        BindingContext = vm;
        InitializeComponent();
	}
}