using WinsorApps.MAUI.CDRE.ViewModels;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.CDRE.Pages;

public partial class EventsListPage : ContentPage
{
	public EventsListPage()
	{
		var vm = new EventListViewModel();
        vm.CreateRequested += Vm_CreateRequested;
        vm.OnError += this.DefaultOnErrorHandler();
		InitializeComponent();
	}

    private void Vm_CreateRequested(object? sender, RecurringEventViewModel e)
    {
        Editor editor = new Editor() { BindingContext = e };
        Navigation.PushAsync(editor);
    }
}