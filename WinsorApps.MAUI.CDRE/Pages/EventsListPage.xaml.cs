using WinsorApps.MAUI.CDRE.ViewModels;
using WinsorApps.MAUI.Shared;

namespace WinsorApps.MAUI.CDRE.Pages;

public partial class EventsListPage : ContentPage
{
    private EventListViewModel ViewModel => (EventListViewModel)BindingContext;
	public EventsListPage(EventListViewModel vm)
	{
        vm.CreateRequested += PushEditor;
        vm.OnError += this.DefaultOnErrorHandler();
        vm.CreateCompleted += Vm_CreateCompleted;
        vm.EditRequested += PushEditor;
		InitializeComponent();
        BindingContext = vm;
    }

    private void Vm_CreateCompleted(object? sender, RecurringEventViewModel e)
    {
        Navigation.PopAsync();
    }

    private void PushEditor(object? sender, RecurringEventViewModel e)
    {
        Editor editor = new Editor(e);
        Navigation.PushAsync(editor);
    }

    private void ContentPage_NavigatedTo(object sender, NavigatedToEventArgs e)
    {
        ViewModel.LoadEvents();
    }
}