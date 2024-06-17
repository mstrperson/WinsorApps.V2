using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.CDRE
{
    public partial class MainPage : ContentPage
    {
       
        public MainPage(
            RegistrarService registrar,
            LocalLoggingService logging,
            CycleDayRecurringEventService cdres)
        {
            MainPageViewModel vm = new(
            [
              new(registrar, "Registrar Data"),
              new(cdres, "Recurring Events")
            ])
            {
                Completion = [
                    ]
            };

            BindingContext = vm;
            vm.OnError += this.DefaultOnErrorHandler();
            LoginPage loginPage = new LoginPage(logging, vm.LoginVM);
            loginPage.OnLoginComplete += (_, _) =>
                Navigation.PopAsync();

            Navigation.PushAsync(loginPage);

            InitializeComponent();
        }

        
    }

}
