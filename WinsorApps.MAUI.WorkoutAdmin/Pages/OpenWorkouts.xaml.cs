using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Athletics.ViewModels;


namespace WinsorApps.MAUI.WorkoutAdmin.Pages;

public partial class OpenWorkouts : ContentPage
{
    public SignInPageViewModel ViewModel => (SignInPageViewModel)BindingContext;


    public OpenWorkouts(SignInPageViewModel vm)
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