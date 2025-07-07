using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class TheaterPage : ContentPage
{
	private TheaterEventViewModel ViewModel
	{
		get => (TheaterEventViewModel)BindingContext;
		set => BindingContext = value;
	}

	public TheaterPage(TheaterEventViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		vm.ReadyToContinue += async (_, _) => await Navigation.PopAsync();
		vm.Deleted += async (_, _) => await Navigation.PopAsync();
		ViewModel = vm;
		InitializeComponent();
	}
}