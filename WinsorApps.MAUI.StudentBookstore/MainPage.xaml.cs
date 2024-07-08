﻿using AsyncAwaitBestPractices;
using WinsorApps.MAUI.StudentBookstore.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.MAUI.Shared.Bookstore.ViewModels;
using System.Runtime.CompilerServices;
using WinsorApps.MAUI.StudentBookstore.Pages;

namespace WinsorApps.MAUI.StudentBookstore
{
    public partial class MainPage : ContentPage
    {

        public MainPage(
            RegistrarService registrar,
            LocalLoggingService logging,
            StudentBookstoreService sbs,
            BookService book)
        {
            MainPageViewModel vm = new(
            [
              new(registrar, "Registrar Data"),
              new(book, "Book Service"),
              new(sbs, "Student Books")
            ])
            {

                #region Service Post Init Tasks
                // when the event service cache is updated, refresh the ViewModelCache as well.
                Completion =
                [
                    new(new(() =>
                    {
                        BookViewModel.Initialize(book, this.DefaultOnErrorAction()).SafeFireAndForget(e => e.LogException());
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

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            var page = ServiceHelper.GetService<RequestedBooksPage>();
            Navigation.PushAsync(page);
        }
    }

}
