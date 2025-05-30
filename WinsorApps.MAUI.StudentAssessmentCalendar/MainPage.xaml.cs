﻿using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.StudentAssessmentCalendar;

public partial class MainPage : ContentPage
{
    public MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;

    public MainPage(
        ApiService api,
        RegistrarService registrar,
        AppService app,
        LocalLoggingService logging,
        StudentAssessmentService studentService,
        CycleDayCollection cycleDays)
    {
        MainPageViewModel vm = new(
        [
            new(registrar, "Registrar Data"),
            new(studentService, "Assessments"),
            new(cycleDays, "Cycle Days"),
            new(app, "Checking for Updates")
        ], app, api, logging)
        {
            AppId = "jKNAXlE8qzLx",
        };

        BindingContext = vm;

        vm.OnError += this.DefaultOnErrorHandler();
        
        LoginPage loginPage = new(logging, vm.LoginVM);
        loginPage.OnLoginComplete += (_, _) =>
        {
            //Navigation.PopAsync();
            vm.UserVM = UserViewModel.Get(api.UserInfo!);

            Navigation.PushAsync(new AppLoadingPage(vm));
        };



        Navigation.PushAsync(loginPage);


        InitializeComponent();
    }

}
