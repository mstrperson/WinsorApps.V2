using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.BookstoreManager
{
    public partial class MainPage : ContentPage
    {
        public MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;


        public MainPage(
        RegistrarService registrar,
        AppService app,
        BookstoreManagerService managerService,
        LocalLoggingService logging)
        {
            InitializeComponent();
        }
    }

}
