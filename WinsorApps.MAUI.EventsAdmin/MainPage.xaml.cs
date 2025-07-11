
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
        EventFormViewModelCacheService cacheService)
    {
        MainPageViewModel vm = new MainPageViewModel(
        [
            new ServiceAwaiterViewModel(registrar, "Registrar Data"), 
            new ServiceAwaiterViewModel(adminService, "Admin Service"),
            new ServiceAwaiterViewModel(eventForms, "Event Forms"),
            new ServiceAwaiterViewModel(calendarService, "Calendar"), 
            new ServiceAwaiterViewModel(budgetCodes, "Budget Codes"), 
            new ServiceAwaiterViewModel(contactService, "Contacts"),
            new ServiceAwaiterViewModel(locationService, "Locations"),
            new ServiceAwaiterViewModel(cateringMenuService, "Catering Services"),
            new ServiceAwaiterViewModel(theaterService, "Theater Services"),
            new ServiceAwaiterViewModel(app, "Checking for Updates"),
            new ServiceAwaiterViewModel(cacheService, "Cache Service"),
        ], 
        app, 
        api, 
        logging)
        {
            Completion = 
            [
                new TaskAwaiterViewModel(LocationViewModel.Initialize(locationService, this.DefaultOnErrorAction()), "Locations Cache"),
                new TaskAwaiterViewModel(BudgetCodeViewModel.Initialize(budgetCodes, this.DefaultOnErrorAction()), "Budget Codes Cache"),
                new TaskAwaiterViewModel(ContactViewModel.Initialize(contactService, this.DefaultOnErrorAction()), "My Contacts"),
                new TaskAwaiterViewModel(ApprovalStatusViewModel.Initialize(eventForms, this.DefaultOnErrorAction()), "Approval Status Cache"),
                new TaskAwaiterViewModel(CateringMenuCategoryViewModel.Initialize(cateringMenuService, this.DefaultOnErrorAction()), "Catering Menus"),
                new TaskAwaiterViewModel(EventTypeViewModel.Initialize(eventForms, this.DefaultOnErrorAction()), "Event Types"),
                new TaskAwaiterViewModel(EventFormViewModel.Initialize(adminService, this.DefaultOnErrorAction()), "Event List")
            ],
            AppId = "yBDj8LA61lpR"
        };

        BindingContext = vm;
        vm.OnError += this.DefaultOnErrorHandler();
        vm.OnCompleted += Vm_OnCompleted;
        LoginPage loginPage = new LoginPage(logging, vm.LoginVM);
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
           var page = ServiceHelper.GetService<MonthlyCalendar>();
           Navigation.PushAsync(page);
        }
    }
}
