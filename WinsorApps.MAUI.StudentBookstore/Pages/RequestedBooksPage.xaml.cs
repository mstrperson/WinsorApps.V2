using WinsorApps.MAUI.StudentBookstore.ViewModels;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.StudentBookstore.Pages;

public partial class RequestedBooksPage : ContentPage
{
	public RequestedBooksPage(StudentBookstoreViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

}