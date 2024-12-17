using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.WorkoutSignIn.ViewModels;

namespace WinsorApps.MAUI.WorkoutSignIn
{
    public partial class MainPage : ContentPage
    {
        public SignInPageViewModel ViewModel => (SignInPageViewModel)BindingContext;


        public MainPage(SignInPageViewModel vm)
        {
            vm.OnError += this.DefaultOnErrorHandler(() => vm.Refresh().SafeFireAndForget(e => e.LogException()));
            this.BindingContext = vm;
            InitializeComponent();
        }

        private void SearchBar_Unfocused(object sender, FocusEventArgs e)
        {
            ViewModel.NewSignIn.StudentSearch.Search();
        }
    }

}
