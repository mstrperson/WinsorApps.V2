using WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;

namespace WinsorApps.MAUI.Helpdesk.Pages.ServiceCase;

public partial class ServiceCaseSearchPage : ContentPage
{
	public ServiceCaseSearchViewModel ViewModel => (ServiceCaseSearchViewModel)BindingContext;

	public ServiceCaseSearchPage()
	{
		InitializeComponent();
		BindingContext = new ServiceCaseSearchViewModel() { ShowFilter = true };
		ViewModel.SelectionMode = SelectionMode.Single;
		ViewModel.Filter.Open = true;
		ViewModel.ShowFilter = false;
        ViewModel.OnSingleResult += ViewModel_OnSingleResult;
	}

    private void ViewModel_OnSingleResult(object? sender, ServiceCaseViewModel e)
    {
		// TODO:  Load Service Case Editor
		Debug.WriteLine($"Selected {e.Id}");
    }
}