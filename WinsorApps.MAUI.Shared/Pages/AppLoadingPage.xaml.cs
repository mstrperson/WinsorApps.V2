using WinsorApps.MAUI.Shared.ViewModels;

namespace WinsorApps.MAUI.Shared.Pages;

public partial class AppLoadingPage : ContentPage
{
	public AppLoadingPage(MainPageViewModel vm)
	{
		this.BindingContext = vm;
		vm.OnCompleted += async (_, _) =>
		{
			await Navigation.PopToRootAsync();
			await vm.InitialContentReady();
		};
		InitializeComponent();
	}
}