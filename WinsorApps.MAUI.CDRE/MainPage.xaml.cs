﻿using AsyncAwaitBestPractices;
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
                ]

                #endregion // Post Init Tasks
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