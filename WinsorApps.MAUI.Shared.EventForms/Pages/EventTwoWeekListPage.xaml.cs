using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;

namespace WinsorApps.MAUI.Shared.EventForms.Pages;

public partial class EventTwoWeekListPage : ContentPage
{
	public EventTwoWeekListPage(EventTwoWeekListPageViewModel vm)
	{
		BindingContext = vm;
		vm.OnError += this.DefaultOnErrorHandler();
		vm.LoadWeeks().SafeFireAndForget(e => e.LogException());
		
        InitializeComponent();
	}
}