using WinsorApps.MAUI.Shared.ViewModels;

namespace WinsorApps.MAUI.Shared.Pages;

public partial class AppLoadingPage : ContentPage
{
	public AppLoadingPage(MainPageViewModel vm)
	{
		this.BindingContext = vm;
		vm.OnCompleted += (_, _) => Navigation.PopAsync();
		InitializeComponent();
	}
}