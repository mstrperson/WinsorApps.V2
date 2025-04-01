using WinsorApps.MAUI.CateringManagement.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.CateringManagement;

public partial class MainPage : ContentPage
{
    public MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;

    public MainPage(
        ApiService api,
        RegistrarService registrar,
        AppService app,
        LocalLoggingService logging,
        CateringEventsPageViewModel eventsPage)
    {
        MainPageViewModel vm = new(
        [
            new(registrar, "Registrar Data"),
            new(app, "Checking for Updates")

        ], app, api, logging)
        {
            Completion = [
                new(eventsPage.Initialize(), "Loading Catering Events")
            ],
            AppId = "axEW8rxZAw1Q"
        };

        BindingContext = vm;
        vm.OnError += this.DefaultOnErrorHandler();
        LoginPage loginPage = new(logging, vm.LoginVM);
        loginPage.OnLoginComplete += (_, _) =>
        {
            //Navigation.PopAsync();
            vm.UserVM = UserViewModel.Get(api.UserInfo!);
            Navigation.PushAsync(new AppLoadingPage(vm));
            vm.LoadReadyContent += Vm_OnCompleted;
        };


        Navigation.PushAsync(loginPage);


        InitializeComponent();
    }

    private void Vm_OnCompleted(object? sender, EventArgs e)
    {
        if (!ViewModel.UpdateAvailable)
        {
            var page = ServiceHelper.GetService<CateringManagementPage>();
            Navigation.PushAsync(page);
        }
    }
}
