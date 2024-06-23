using WinsorApps.MAUI.CDRE.ViewModels;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.CDRE.Pages;

public partial class EventsListPage : ContentPage
{
	public EventsListPage(EventListViewModel vm)
	{
        vm.CreateRequested += Vm_CreateRequested;
        vm.OnError += this.DefaultOnErrorHandler();
		InitializeComponent();
        BindingContext = vm;
    }

    private void Vm_CreateRequested(object? sender, RecurringEventViewModel e)
    {
        Editor editor = new Editor(e);
        Navigation.PushAsync(editor);
    }
}