using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherBookOrders;

public partial class MainPage : ContentPage
{
    MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;

    public MainPage(
        ApiService api,
        RegistrarService registrar,
        AppService app,
        LocalLoggingService logging,
        TeacherBookstoreService teacherService,
        BookService bookService)
    {
        MainPageViewModel vm = new(
        [
            new(registrar, "Registrar Data"),
            new(bookService, "Books Database"),
            new(teacherService, "Teacher Bookstore"),
            new(app, "Checking for Updates")

        ], app, api, logging)
        {
            Completion = [
            ],
            AppId = "wQ0v6D1Z3YNn"
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
            //var page = ServiceHelper.GetService<MyAssessmentsPage>();
            //Navigation.PushAsync(page);
        }
    }
}
