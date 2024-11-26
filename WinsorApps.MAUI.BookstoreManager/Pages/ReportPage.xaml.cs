using WinsorApps.MAUI.BookstoreManager.ViewModels;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.BookstoreManager.Pages;

public partial class ReportPage : ContentPage
{
	public ReportPage(ReportsPageViewModel vm)
	{
		BindingContext = vm;
		vm.OnError += this.DefaultOnErrorHandler();
		InitializeComponent();
	}
}