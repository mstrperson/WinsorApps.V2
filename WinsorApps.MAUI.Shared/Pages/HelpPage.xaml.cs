using WinsorApps.MAUI.Shared.ViewModels;

namespace WinsorApps.MAUI.Shared.Pages;

public partial class HelpPage : ContentPage
{
	public HelpPage(HelpPageViewModel vm)
	{
		BindingContext = vm;
		InitializeComponent();
	}
}