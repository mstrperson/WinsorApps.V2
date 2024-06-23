using WinsorApps.MAUI.CDRE.ViewModels;

namespace WinsorApps.MAUI.CDRE.Pages;

public partial class Editor : ContentPage
{
	RecurringEventViewModel ViewModel => (RecurringEventViewModel)BindingContext; 

	public Editor(RecurringEventViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}