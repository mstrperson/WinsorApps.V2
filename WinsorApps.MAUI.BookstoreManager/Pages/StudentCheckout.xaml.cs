using WinsorApps.MAUI.BookstoreManager.ViewModels;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.BookstoreManager.Pages;

public partial class StudentCheckout : ContentPage
{
	public StudentCheckout(StudentPageViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		BindingContext = vm;
		InitializeComponent();
	}
}