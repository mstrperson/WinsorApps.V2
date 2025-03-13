
using WinsorApps.MAUI.EventsAdmin.Pages;
using WinsorApps.MAUI.EventsAdmin.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.EventForms.Services.Admin;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.EventsAdmin;

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
        TheaterService theaterService,
        EventsAdminService adminService,
        EventListPageViewModel listpagevm)
    {
        MainPageViewModel vm = new(
        [
            new(registrar, "Registrar Data"),
            new(adminService, "Admin Service"),
            new(eventForms, "Event Forms"),
            new(calendarService, "Calendar"),
            new(budgetCodes, "Budget Codes"),
            new(contactService, "Contacts"),
            new(locationService, "Locations"),
            new(cateringMenuService, "Catering Services"),
            new(theaterService, "Theater Services"),
            new(app, "Checking for Updates")

        ], app, api, logging)
        {
            Completion = 
            [
                new(LocationViewModel.Initialize(locationService, this.DefaultOnErrorAction()), "Locations Cache"),
                new(BudgetCodeViewModel.Initialize(budgetCodes, this.DefaultOnErrorAction()), "Budget Codes Cache"),
                new(ContactViewModel.Initialize(contactService, this.DefaultOnErrorAction()), "My Contacts"),
                new(ApprovalStatusViewModel.Initialize(eventForms, this.DefaultOnErrorAction()), "Approval Status Cache"),
                new(CateringMenuCategoryViewModel.Initialize(cateringMenuService, this.DefaultOnErrorAction()), "Catering Menus"),
                new(EventTypeViewModel.Initialize(eventForms, this.DefaultOnErrorAction()), "Event Types"),
                new(listpagevm.Initialize(this.DefaultOnErrorAction()), "Event List")
            ],
            AppId = "yBDj8LA61lpR"
        };

        BindingContext = vm;
        vm.OnError += this.DefaultOnErrorHandler();
        vm.OnCompleted += Vm_OnCompleted;
        LoginPage loginPage = new(logging, vm.LoginVM);
        loginPage.OnLoginComplete += (_, _) =>
        {
            Navigation.PopAsync();
            vm.UserVM = UserViewModel.Get(api.UserInfo!);
        };
        Navigation.PushAsync(loginPage);


        InitializeComponent();
    }

    private void Vm_OnCompleted(object? sender, EventArgs e)
    {
        if (!ViewModel.UpdateAvailable)
        {
           var page = ServiceHelper.GetService<EventListPage>();
           Navigation.PushAsync(page);
        }
    }
}
