using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class FormEditor : ContentPage
{
	public EventFormViewModel ViewModel => (EventFormViewModel)BindingContext;

	public FormEditor()
	{
		ViewModel.OnError += (sender, err) => this.DefaultOnErrorHandler();
		ViewModel.MarCommRequested += (sender, vm) => Navigation.PushAsync(new MarComPage() { BindingContext = vm });
		ViewModel.TheaterRequested += (sender, vm) => Navigation.PushAsync(new TheaterPage() { BindingContext = vm });
		ViewModel.TechRequested += (sender, vm) => Navigation.PushAsync(new TechPage() { BindingContext = vm });
		ViewModel.CateringRequested += (sender, vm) => Navigation.PushAsync(new CateringPage(vm));
		ViewModel.FieldTripRequested += (sender, vm) => Navigation.PushAsync(new FieldTripPage() { BindingContext = vm });
		ViewModel.FacilitesRequested += (sender, vm) => Navigation.PushAsync(new FacilitesPage() { BindingContext = vm });

		ViewModel.TemplateRequested += async (sender, vm) =>
		{
			await Navigation.PopToRootAsync();
			await Navigation.PushAsync(new FormEditor() { BindingContext = vm });
		};

        InitializeComponent();
	}

}