using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class FormView : ContentPage
{
    public FormView(EventFormViewModel viewModel)
    {
        BindingContext = viewModel;
        if(viewModel.HasFacilities)
            viewModel.LoadFacilities().SafeFireAndForget(e => e.LogException());
        if(viewModel.HasCatering)
            viewModel.LoadCatering().SafeFireAndForget(e => e.LogException());
        if(viewModel.HasTech)
            viewModel.LoadTech().SafeFireAndForget(e => e.LogException());
        if(viewModel.HasTheater)
            viewModel.LoadTheater().SafeFireAndForget(e => e.LogException());
        if(viewModel.HasMarComm)
            viewModel.LoadMarComm().SafeFireAndForget(e => e.LogException());
        if (viewModel.IsFieldTrip)
            viewModel.LoadFieldTrip().SafeFireAndForget(e => e.LogException());
        
        viewModel.TemplateRequested += (_, vm) =>
        {
            FormEditor page = new(vm);
            Navigation.PushAsync(page);
        };
        
        InitializeComponent();
    }
}