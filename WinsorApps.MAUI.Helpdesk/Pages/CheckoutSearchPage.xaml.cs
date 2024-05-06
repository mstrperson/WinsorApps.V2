using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Storage;
using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.Pages;

public partial class CheckoutSearchPage : ContentPage
{
    private CheckoutSearchViewModel ViewModel => (CheckoutSearchViewModel)BindingContext;

	public CheckoutSearchPage(CheckoutSearchViewModel vm)
	{
		InitializeComponent();
        vm.OnError += this.DefaultOnErrorHandler();
        vm.OnExport += Vm_OnExport;
        vm.OnZeroResults += Vm_OnZeroResults;
        this.BindingContext = vm;
    }

    private void Vm_OnZeroResults(object? sender, EventArgs e)
    {
        this.Splash(() =>
        {
            var spvm = new SplashPageViewModel("No Results",
                [$"{ViewModel.SearchText} did not return any results in Open Checkouts"], TimeSpan.FromSeconds(15));
            spvm.OnClose += (_, _) => { ViewModel.Clear(); };
            return spvm;
        });
    }

    private void Vm_OnExport(object? sender, (byte[], string) e)
    {
        (var data, var fileName) = e;
        var task = FileSaver.SaveAsync(fileName, new MemoryStream(data));
        task.SafeFireAndForget(e => e.LogException());
    }
}