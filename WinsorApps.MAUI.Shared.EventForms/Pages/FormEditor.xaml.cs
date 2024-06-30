using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class FormEditor : ContentPage
{
	public EventFormViewModel ViewModel => (EventFormViewModel)BindingContext;

	public FormEditor(EventFormViewModel vm)
	{
        vm.TemplateRequested += async (sender, vm) =>
        {
            await Navigation.PopToRootAsync();
            await Navigation.PushAsync(new FormEditor(vm));
        };

        BindingContext = vm;
        InitializeComponent();
	}

}