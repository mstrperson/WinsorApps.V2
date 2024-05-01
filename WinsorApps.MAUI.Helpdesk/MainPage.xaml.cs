using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk;

public partial class MainPage : ContentPage
{
    
    
    public MainPage(
        RegistrarService registrar, 
        AppService app, 
        DeviceService devService, 
        JamfService jamfService, 
        ServiceCaseService caseService,
        LocalLoggingService logging)
    {
        MainPageViewModel vm = new(
        [
            new(registrar, "Registrar Data"),
            new(devService, "Device Service"),
            new(jamfService, "Jamf Service"),
            new(caseService, "Service Cases")
        ]);

        BindingContext = vm;
        LoginPage loginPage = new LoginPage(logging, vm.LoginVM);
        loginPage.OnLoginComplete += (_, _) => 
            Navigation.PopAsync();

        Navigation.PushAsync(loginPage);

        
        InitializeComponent();
    }
}