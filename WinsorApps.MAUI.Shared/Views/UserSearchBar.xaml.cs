using AsyncAwaitBestPractices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Views;

public partial class UserSearchBar : ContentView
{
    public static BindableProperty SelectedUserProperty = 
        BindableProperty.Create(nameof(SelectedUser), typeof(UserViewModel), typeof(UserSearchBar),
            IEmptyViewModel<UserViewModel>.Empty, BindingMode.TwoWay);
    
    public UserViewModel SelectedUser
    {
        get => (UserViewModel)GetValue(SelectedUserProperty);
        set
        {
            ViewModel?.Select(value);
            SetValue(SelectedUserProperty, value);
        }
    }

    public UserSearchViewModel? ViewModel => (UserSearchViewModel) BindingContext;
    
    private readonly RegistrarService _registrar;
    public UserSearchBar()
    {
        _registrar = ServiceHelper.GetService<RegistrarService>();

        WaitForInit().SafeFireAndForget();
        
        InitializeComponent();
    }

    private async Task WaitForInit()
    {
        while (!_registrar.Ready)
        {
            await Task.Delay(250);
        }

        this.BindingContext = new UserSearchViewModel();
    }
}