﻿using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk;

public partial class MainPage : ContentPage
{
    private readonly DeviceService _deviceService;
    private readonly CheqroomService _cheqroom;
    private readonly ServiceCaseService _caseService;

    public MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;

    public MainPage(
        RegistrarService registrar, 
        AppService app, 
        DeviceService devService, 
        JamfService jamfService, 
        ServiceCaseService caseService,
        CheqroomService cheqroom,
        LocalLoggingService logging)
    {
        MainPageViewModel vm = new(
        [
            new(registrar, "Registrar Data"),
            new(devService, "Device Service"),
            new(jamfService, "Jamf Service"),
            new(cheqroom, "Cheqroom Checkouts"),
            new(caseService, "Service Cases")
        ])
        {
            Completion = [
                new(new Task(async () => await DeviceViewModel.Initialize(devService, this.DefaultOnErrorAction())), "Device Cache"),
                new(new Task(async () => await ServiceCaseViewModel.Initialize(caseService, this.DefaultOnErrorAction())), "Service Case Cache"),
                new(new Task(async () => await CheqroomItemViewModel.Initialize(cheqroom, this.DefaultOnErrorAction())), "Cheqroom Cache"),
                new(new Task(async () => await CheckoutSearchResultViewModel.Initialize(cheqroom, this.DefaultOnErrorAction())), "Checkout Cache")
            ]
        };

        BindingContext = vm;
        vm.OnError += this.DefaultOnErrorHandler();
        vm.OnCompleted += Vm_OnCompleted;
        LoginPage loginPage = new LoginPage(logging, vm.LoginVM);
        loginPage.OnLoginComplete += (_, _) => 
            Navigation.PopAsync();

        Navigation.PushAsync(loginPage);

        
        InitializeComponent();
        _deviceService = devService;
        _cheqroom = cheqroom;
        _caseService = caseService;
    }

    private void Vm_OnCompleted(object? sender, EventArgs e)
    {
    }
}