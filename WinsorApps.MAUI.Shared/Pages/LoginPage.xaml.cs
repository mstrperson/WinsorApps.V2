using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Microsoft.Maui.Controls;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LocalLoggingService _logging;
    
    public LoginViewModel ViewModel => (LoginViewModel) BindingContext;
    
    public LoginPage(LocalLoggingService logging)
    {
        _logging = logging;
        var viewModel = new LoginViewModel();
        viewModel.OnLogin += ViewModelOnOnLogin;
        viewModel.OnLogout += ViewModelOnOnLogout;
        viewModel.OnForgotPassword += ViewModelOnOnForgotPassword;
        viewModel.OnError += ViewModelOnOnError;
        BindingContext = viewModel;
        InitializeComponent();
    }

    private void ViewModelOnOnError(object? sender, ErrorRecord e)
    {
        _logging.LogMessage(LocalLoggingService.LogLevel.Error, e.type, e.error);
        ViewModel.StatusMessage = e.error;
        StatusLabel.IsVisible = true;
    }

    private void ViewModelOnOnForgotPassword(object? sender, string e)
    {
        ViewModel.StatusMessage = e;
        StatusLabel.IsVisible = true;
    }

    private void ViewModelOnOnLogout(object? sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;
    }

    private void ViewModelOnOnLogin(object? sender, EventArgs e)
    {/*
        try
        {
            Navigation.PopAsync().SafeFireAndForget(ex => ex.LogException(_logging));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }*/
        
    }
}