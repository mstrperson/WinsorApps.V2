using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.WorkoutAdmin.ViewModels;

namespace WinsorApps.MAUI.WorkoutAdmin
{
    public partial class MainPage : ContentPage
    {
        public MainPage(ReportBuilderViewModel vm)
        {
            BindingContext = vm;
            vm.OnError += this.DefaultOnErrorHandler(async () => await vm.RequestLogs());
            InitializeComponent();
        }

    }

}
