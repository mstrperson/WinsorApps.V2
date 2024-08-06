using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.MAUI.StudentAssessmentCalendar.Pages;
using WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;
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
        CycleDayCollection cycleDays,
        WeeklyViewModel weeklyViewModel)
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
            Completion = [
                new (weeklyViewModel.Initialize(this.DefaultOnErrorAction()), "Weekly View")
            ]
        };

        BindingContext = vm;

        vm.OnError += this.DefaultOnErrorHandler();
        
        LoginPage loginPage = new LoginPage(logging, vm.LoginVM);
        loginPage.OnLoginComplete += (_, _) =>
        {
            Navigation.PopAsync();
            vm.UserVM = UserViewModel.Get(api.UserInfo!.Value);
        };
        Navigation.PushAsync(loginPage);


        InitializeComponent();
    }

}
