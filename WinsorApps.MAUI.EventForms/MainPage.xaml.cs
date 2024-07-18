using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.EventForms;

public partial class MainPage : ContentPage
{

    public MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;

    public MainPage(
        ApiService api,
        RegistrarService registrar,
        AppService app,
        LocalLoggingService logging,
        EventFormsService eventForms,
        ReadonlyCalendarService calendarService,
        BudgetCodeService budgetCodes,
        ContactService contactService,
        LocationService locationService,
        CateringMenuService cateringMenuService,
        TheaterService theaterService)
    {
        MainPageViewModel vm = new(
        [
            new(registrar, "Registrar Data"),
            new(eventForms, "Event Forms"),
            new(calendarService, "Calendar"),
            new(budgetCodes, "Budget Codes"),
            new(contactService, "Contacts"),
            new(locationService, "Locations"),
            new(cateringMenuService, "Catering Services"),
            new(theaterService, "Theater Services")

        ], app, api)
        {
            Completion = [
                new(new Task(async () => await EventFormViewModel.Initialize(eventForms, this.DefaultOnErrorAction())), "Event Forms Cache"),
                new(new Task(async () => await LocationViewModel.Initialize(locationService, this.DefaultOnErrorAction())), "Locations Cache"),
                new(new Task(async () => await BudgetCodeViewModel.Initialize(budgetCodes, this.DefaultOnErrorAction())), "Budget Codes Cache"),
                new(new Task(async () => await ContactViewModel.Initialize(contactService, this.DefaultOnErrorAction())), "My Contacts"),
                new(new Task(async () => await ApprovalStatusViewModel.Initialize(eventForms, this.DefaultOnErrorAction())), "Approval Status Cache"),
                new(new Task(async () => await CateringMenuCategoryViewModel.Initialize(cateringMenuService, this.DefaultOnErrorAction())), "Catering Menus"),
                new(new Task(async () => await EventTypeViewModel.Initialize(eventForms, this.DefaultOnErrorAction())), "Event Types")
            ]
        };

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
    }
}
