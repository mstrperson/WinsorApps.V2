using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using PropertyChangingEventArgs = System.ComponentModel.PropertyChangingEventArgs;

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
                var page = new FormEditor(template);
                Navigation.PushAsync(page);
            };

            vm.OnError += this.DefaultOnErrorHandler();
            vm.Deleted += async (_, _) => await Navigation.PopToRootAsync();
            vm.Submitted += async (_, _) => await Navigation.PopToRootAsync();
            vm.MarCommRequested += (sender, mvm) => Navigation.PushAsync(new MarComPage(mvm));
            vm.TheaterRequested += (sender, thvm) => Navigation.PushAsync(new TheaterPage(thvm));
            vm.TechRequested += (sender, tvm) => Navigation.PushAsync(new TechPage(tvm));
            vm.CateringRequested += (sender, cvm) => Navigation.PushAsync(new CateringPage(cvm));
            vm.FieldTripRequested += (sender, ftvm) => Navigation.PushAsync(new FieldTripPage(ftvm));
            vm.FacilitesRequested += (sender, fvm) => Navigation.PushAsync(new FacilitesPage(fvm));

            
            vm.PropertyChanged += Vm_PropertyChanged;

            vm.HasLoadedOnce = true;
        }

        vm.CanEditBase = vm.IsNew || vm.IsUpdating || vm.IsCreating;
        vm.CanEditSubForms = vm is { CanEditBase: true, IsNew: false };

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