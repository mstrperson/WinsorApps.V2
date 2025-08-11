using WinsorApps.MAUI.EventsAdmin.ViewModels.Catering;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.EventsAdmin.Pages;

public partial class CateringMenuEditor : ContentPage
{
    private CateringMenuEditorPageViewModel ViewModel => (CateringMenuEditorPageViewModel)BindingContext;

    public CateringMenuEditor(CateringMenuEditorPageViewModel vm)
	{
		BindingContext = vm;
		vm.OnError += this.DefaultOnErrorHandler();
		vm.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == "PriceChangeEffectiveDate")
			{
				foreach(var menu in vm.AllMenus.AllMenus)
				{
					foreach(var item in menu.Items.AllItems)
						item.PriceChangeEffectiveDate = vm.PriceChangeEffectiveDate;
                }
            }
		};

        InitializeComponent();
	}

    private void OnDragStarting(object sender, DragStartingEventArgs e)
    {
        var label = (DragGestureRecognizer)sender;


        if (label.BindingContext is CateringMenuItemViewModel vm)
        {
            e.Data.Properties["DraggedOrdinal"] = vm.Ordinal;
            e.Data.Properties["DraggedId"] = vm.Id;
        }
    }

    private void OnDrop(object sender, DropEventArgs e)
    {
        if (sender is DropGestureRecognizer grid && grid.BindingContext is CateringMenuItemViewModel targetVm)
        {
            if (e.Data.Properties.TryGetValue("DraggedOrdinal", out var draggedOrdinalObj) &&
                e.Data.Properties.TryGetValue("DraggedId", out var draggedIdObj) &&
                int.TryParse(draggedOrdinalObj?.ToString(), out var draggedOrdinal) &&
                draggedIdObj is string draggedId)
            {
                var draggedItem = ViewModel.SelectedMenu.Items.AllItems
                    .FirstOrDefault(item => item.Item.Id == draggedId);

                if (draggedItem is null)
                    return;


                ViewModel.SelectedMenu.MoveItemOrdinal(draggedId, targetVm.Ordinal)
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            this.DefaultOnErrorAction()(new ErrorRecord(task.Exception.Message, "Failed to move item ordinal."));
                        }
                    });
            }
        }
    }
}