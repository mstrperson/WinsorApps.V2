using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Helpdesk.Pages;

public partial class CheqroomQuickTaskPage : ContentPage
{
	private readonly LocalLoggingService _logging;
    private CheqroomQuickTasksViewModel ViewModel => (CheqroomQuickTasksViewModel)BindingContext;
    public CheqroomQuickTaskPage(CheqroomQuickTasksViewModel vm, LocalLoggingService logging)
    {
        _logging = logging;
        vm.OnError += this.DefaultOnErrorHandler();
        vm.QuickCheckout.OnError += this.DefaultOnErrorHandler();
        vm.CheckoutSearch.OnError += this.DefaultOnErrorHandler();
        vm.QuickCheckout.OnCheckoutSuccessful += (sender, e) =>
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information,
                $"{e.Items.DelimeteredList(", ")} was successfully checked out to {vm.QuickCheckout.UserSearch.Selected.DisplayName}");
        };
        vm.CheckoutSearch.OnSingleResult += CheckoutSearch_OnSingleResult;
        BindingContext = vm;
        InitializeComponent();
    }

    private void CheckoutSearch_OnSingleResult(object? sender, CheckoutSearchResultViewModel checkout)
    {
        var task = checkout.CheckIn();
        task.WhenCompleted(() =>
        {
            if(task.Result)
            {
                this.Splash(() =>
                {
                    var spvm = new SplashPageViewModel("Check In Successful",
                        [$"{checkout.Items.DelimeteredList()} successfully Checked In for {checkout.User.DisplayName}"]);
                    spvm.OnClose += async (_, _) =>
                    {
                        ViewModel.CheckoutSearch.Clear();
                        await ViewModel.CheckoutSearch.Refresh();
                    };
                    return spvm;
                });
            }
        });
    }
}