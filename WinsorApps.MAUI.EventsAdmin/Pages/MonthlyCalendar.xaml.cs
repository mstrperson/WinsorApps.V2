using System;
using AsyncAwaitBestPractices;
using Microsoft.Maui.Controls;
using WinsorApps.MAUI.EventsAdmin.ViewModels;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.Pages;

namespace WinsorApps.MAUI.EventsAdmin.Pages;

public partial class MonthlyCalendar : ContentPage
{
	private AdminCalendarViewModel ViewModel => (AdminCalendarViewModel)BindingContext;
	public MonthlyCalendar(AdminCalendarViewModel viewModel)
	{
		BindingContext = viewModel;
		viewModel.Calendar.EventSelected += (_, vm) =>
		{
			FormView page = new(vm);
			Navigation.PushAsync(page);
		};

		InitializeComponent();
	}

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
    }

    private void VisualElement_OnLoaded(object? sender, EventArgs e)
    {
	    if (ViewModel.HasLoaded) return;
			
	    ViewModel.Busy = true;
	    ViewModel.BusyMessage = "Loading...";
	    ViewModel.Refresh().SafeFireAndForget(ex => ex.LogException());
    }
}