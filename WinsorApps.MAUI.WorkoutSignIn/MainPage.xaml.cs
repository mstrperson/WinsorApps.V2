using WinsorApps.MAUI.WorkoutSignIn.ViewModels;

namespace WinsorApps.MAUI.WorkoutSignIn
{
    public partial class MainPage : ContentPage
    {
        WorkoutListPageViewModel ViewModel => (WorkoutListPageViewModel)BindingContext;
        public MainPage(WorkoutListPageViewModel vm)
        {
            BindingContext = vm;
            InitializeComponent();
        }
        
        private void SearchBar_Unfocused(object sender, FocusEventArgs e)
        {
            ViewModel.SignInViewModel.StudentSearch.Search();
        }
    }

}
