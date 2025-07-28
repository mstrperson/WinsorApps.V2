using WinsorApps.MAUI.EventsAdmin.ViewModels.Catering;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.EventsAdmin.Pages;

public partial class CateringAdminEventListPage : ContentPage
{
	public CateringAdminEventListPage(CateringManagementEventListPageViewModel vm)
	{
		BindingContext = vm;
		vm.OnError += this.DefaultOnErrorHandler();
		InitializeComponent();
	}
}