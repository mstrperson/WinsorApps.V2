using WinsorApps.MAUI.CDRE.Pages;
using WinsorApps.MAUI.CDRE.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.CDRE
{
    public partial class MainPage : ContentPage
    {
       
        public MainPage(
            AppService app,
            ApiService api,
            RegistrarService registrar,
            LocalLoggingService logging,
            CycleDayRecurringEventService cdres)
        {
            MainPageViewModel vm = new(
            [
              new(registrar, "Registrar Data"),
              new(cdres, "Recurring Events"),
              new(app, "Checking for Updates")
            ], app, api, logging)
            {

                #region Service Post Init Tasks
                // when the event service cache is updated, refresh the ViewModelCache as well.
                Completion = 
                [
                    new(new(() =>
                    {
                        RecurringEventViewModel.Initialize(cdres, this.DefaultOnErrorAction()).SafeFireAndForget(e => e.LogException());
                        cdres.OnCacheRefreshed +=
                        (_, _) =>
                        {
                            RecurringEventViewModel.Initialize(cdres, this.DefaultOnErrorAction()).SafeFireAndForget(e => e.LogException());
                        };
                    }), "Loading Recurring Event Cache")
                ],

                #endregion // Post Init Tasks
                AppId = "al9q8gMZOyNw"
            };

            BindingContext = vm;
            vm.OnError += this.DefaultOnErrorHandler();
            vm.OnCompleted += Vm_OnCompleted;
            LoginPage loginPage = new(logging, vm.LoginVM);
            loginPage.OnLoginComplete += (_, _) =>
                Navigation.PopAsync();

            Navigation.PushAsync(loginPage);

            InitializeComponent();

        }

        public MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;
        private void Vm_OnCompleted(object? sender, EventArgs e)
        {
            if (!ViewModel.UpdateAvailable)
            {
                var page = ServiceHelper.GetService<EventsListPage>();
                Navigation.PushAsync(page);
            }
        }
    }

}
