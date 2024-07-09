using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class FormEditor : ContentPage
{
	public EventFormViewModel ViewModel => (EventFormViewModel)BindingContext;

	public FormEditor(EventFormViewModel vm)
	{
        if (!vm.HasLoadedOnce)
        {
            vm.TemplateRequested += (sender, template) =>
            {
                Navigation.PopToRootAsync();
                var page = new FormEditor(template);
                MainThread.BeginInvokeOnMainThread(() => Navigation.PushAsync(page));
            };

            vm.OnError += this.DefaultOnErrorHandler();
            vm.Deleted += async (_, _) => await Navigation.PopToRootAsync();
            vm.Submitted += async (_, _) => await Navigation.PopToRootAsync();

            vm.PropertyChanged += Vm_PropertyChanged;

            vm.HasLoadedOnce = true;
        }

        InitializeComponent();
        BindingContext = vm;
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if(e.PropertyName == "StartDate")
        {
            ViewModel.EndDate = ViewModel.StartDate;
        }
    }
}