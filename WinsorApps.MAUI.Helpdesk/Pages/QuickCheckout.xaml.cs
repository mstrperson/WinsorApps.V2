using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.Pages;

public partial class QuickCheckout : ContentPage
{
	public QuickCheckout(CheqroomService cheqroom)
	{
		var vm = new QuickCheckoutViewModel(cheqroom);
		vm.OnError += this.DefaultOnErrorHandler();
		vm.OnCheckoutSuccessful += (sender, e) =>
		{
			ServiceHelper.GetService<LocalLoggingService>().LogMessage(LocalLoggingService.LogLevel.Information,
				$"{vm.AssetTag} was successfully checked out to {vm.UserSearch.Selected.DisplayName}");
		};
		BindingContext = vm;
		InitializeComponent();
	}
}