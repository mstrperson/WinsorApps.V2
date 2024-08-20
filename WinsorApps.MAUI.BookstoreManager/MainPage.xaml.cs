using WinsorApps.MAUI.BookstoreManager.Pages;
using WinsorApps.MAUI.BookstoreManager.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.BookstoreManager;

public partial class MainPage : ContentPage
{

    public EventHandler<ErrorRecord>? OnError;
    public MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;


    public MainPage(
        RegistrarService registrar,
        ApiService api,
        AppService app,
        BookstoreManagerService managerService,
        BookService bookService,
        LocalLoggingService logging,
        StudentPageViewModel studentPage)
    {
        MainPageViewModel vm = new(
        [
            new(registrar, "Registrar Data"),
            new(managerService, "Manager Service"),
            new(bookService, "Books"),
            new(studentPage, "Student Orders"),
            new(app, "Checking for Updates")
        ], app, api, logging)
        {
            Completion = [
            ],
            AppId = "PwpjXEMXEv5K"
        };

        this.DefaultOnErrorAction();

        BindingContext = vm;
        vm.OnError += this.DefaultOnErrorHandler();
        vm.OnCompleted += Vm_OnCompleted;
        LoginPage loginPage = new LoginPage(logging, vm.LoginVM);
        loginPage.OnLoginComplete += (_, _) =>
        {
            Navigation.PopAsync();
            vm.UserVM = UserViewModel.Get(api.UserInfo!.Value);
        };
        Navigation.PushAsync(loginPage);


        InitializeComponent();
    }

    private void Vm_OnCompleted(object? sender, EventArgs e)
    {
        var page = ServiceHelper.GetService<StudentCheckout>();
        Navigation.PushAsync(page);
    }

}