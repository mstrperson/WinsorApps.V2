using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.WorkoutSignIn
{
    public partial class MainPage : ContentPage
    {
        public SignInPageViewModel ViewModel => (SignInPageViewModel)BindingContext;


        public MainPage(SignInPageViewModel vm)
        {
            vm.OnError += this.DefaultOnErrorHandler(() => vm.Refresh().SafeFireAndForget(e => e.LogException()));
            this.BindingContext = vm;
            vm.PropertyChanged += Vm_PropertyChanged;
            InitializeComponent();
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.ShowNewSignin) && ViewModel.ShowNewSignin) 
            {
                SignInSearch.Focus();
            }
        }

        private void SearchBar_Unfocused(object sender, FocusEventArgs e)
        {
            if (!string.IsNullOrEmpty(SignInSearch.Text))
            {
                ViewModel.NewSignIn.StudentSearch.Search();
            }
            else
            {
                ViewModel.ShowNewSignin = false;
            }
        }
    }

}
