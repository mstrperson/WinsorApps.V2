using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;
using WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar
{
    public partial class MainPage : ContentPage
    {

        public MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;

        public MainPage(
            ApiService api,
            RegistrarService registrar,
            AppService app,
            LocalLoggingService logging,
            CycleDayCollection cycleDays,
            ReadonlyCalendarService calendarService,
            TeacherAssessmentService teacherAssessmentService,
            MyAssessmentsCollectionViewModel myAssessmentsCollectionViewModel,
            MyAssessmentsPageViewModel myAssessmentsPageViewModel,
            AllMyStudentsViewModel allMyStudents,
            MonthlyCalendarViewModel monthly)
        {
            MainPageViewModel vm = new(
            [
                new(registrar, "Registrar Data"),
                new(cycleDays, "Cycle Days"),
                new(calendarService, "Calendar Info"),
                new(teacherAssessmentService, "My Assessments"),
                new(app, "Checking for Updates")

            ], app, api, logging)
            {
                Completion = [
                    new(myAssessmentsCollectionViewModel.Initialize(this.DefaultOnErrorAction()), "My Assessments"),
                    new(myAssessmentsPageViewModel.Initialize(this.DefaultOnErrorAction()), "Assessments Page"),
                    new(allMyStudents.Initialize(this.DefaultOnErrorAction()), "My Students"),
                    new(monthly.Initialize(this.DefaultOnErrorAction()), "Monthly Calendar")
                ],
                AppId = "V71r6vDXOgD2"
            };

            BindingContext = vm;
            vm.OnError += this.DefaultOnErrorHandler();
            LoginPage loginPage = new LoginPage(logging, vm.LoginVM);
            loginPage.OnLoginComplete += (_, _) =>
            {
                //Navigation.PopAsync();
                vm.UserVM = UserViewModel.Get(api.UserInfo!.Value);
                Navigation.PushAsync(new AppLoadingPage(vm));
                vm.OnCompleted += Vm_OnCompleted;
            };


            Navigation.PushAsync(loginPage);


            InitializeComponent();
        }

        private void Vm_OnCompleted(object? sender, EventArgs e)
        {
            if (!ViewModel.UpdateAvailable)
            {
                var page = ServiceHelper.GetService<MyAssessmentsPage>();
                Navigation.PushAsync(page);
            }
        }
    }

}
