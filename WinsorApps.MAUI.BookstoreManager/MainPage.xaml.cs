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
        AppService app,
        BookstoreManagerService managerService,
        BookService bookService,
        LocalLoggingService logging)
    {
        MainPageViewModel vm = new(
        [
            new(registrar, "Registrar Data"),
            new(managerService, "Manager Service"),
            new(bookService, "Books")
        ])
        {
            Completion = [

            ]
        };

        this.DefaultOnErrorAction();

        BindingContext = vm;
        vm.OnError += this.DefaultOnErrorHandler();
        vm.OnCompleted += Vm_OnCompleted;
        LoginPage loginPage = new LoginPage(logging, vm.LoginVM);
        loginPage.OnLoginComplete += (_, _) =>
            Navigation.PopAsync();

        Navigation.PushAsync(loginPage);


        InitializeComponent();
    }

    private void Vm_OnCompleted(object? sender, EventArgs e)
    {
    }

}