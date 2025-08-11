using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels.Catering;

public partial class CateringItemCollectionViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<AdminCateringMenuItemViewModel>? ItemSelected;
    public event EventHandler<AdminCateringMenuItemViewModel>? ItemDeleted;

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] ObservableCollection<AdminCateringMenuItemViewModel> allItems = [];
    [ObservableProperty] ObservableCollection<AdminCateringMenuItemViewModel> visibleItems = [];
    [ObservableProperty] bool showDeleted = false;

    public CateringItemCollectionViewModel(IEnumerable<CateringMenuItem> models)
    {
        AllItems = 
        [..
            models
                .Select(AdminCateringMenuItemViewModel.Get)
                .OrderBy(it => it.Item.Ordinal)
        ];

        foreach (var item in AllItems)
        {
            item.Selected += (_, it) => ItemSelected?.Invoke(this, it);
            item.Deleted += (_, it) =>
            {
                item.Item.Deleted = true;
                foreach (var otherItem in AllItems.Where(i => i.Item.Ordinal > item.Item.Ordinal))
                    otherItem.Item.Ordinal--;

                VisibleItems = ShowDeleted
                    ? [.. AllItems]
                    : [.. AllItems.Where(i => !i.Item.Deleted).OrderBy(i => i.Item.Ordinal)];
                ItemDeleted?.Invoke(this, it);
            };
            item.OnError += (sender, error) => OnError?.Invoke(sender, error);

            item.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }

        VisibleItems = [.. AllItems.Where(it => !it.Item.Deleted).OrderBy(it => it.Item.Ordinal)];
    }

    [RelayCommand]
    public void ToggleShowDeleted()
    {
        ShowDeleted = !ShowDeleted;
        VisibleItems = ShowDeleted
            ? [.. AllItems]
            : [.. AllItems.Where(m => !m.Item.Deleted)];
    }
}


public partial class AdminCateringMenuItemViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    ISelectable<AdminCateringMenuItemViewModel>
{
    private static readonly CateringMenuService _service = ServiceHelper.GetService<CateringMenuService>();
    [ObservableProperty] CateringMenuItemViewModel item = CateringMenuItemViewModel.Empty;

    [ObservableProperty] DateTime priceChangeEffectiveDate = DateTime.Today;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool isSelected;
    [ObservableProperty] string menuId;
    [ObservableProperty] bool showNameEdit;
    [ObservableProperty] bool showCostEdit;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AdminCateringMenuItemViewModel>? Selected;
    public event EventHandler<AdminCateringMenuItemViewModel>? Deleted;
    public event EventHandler<AdminCateringMenuItemViewModel>? Saved;
    public AdminCateringMenuItemViewModel(string menuId) { MenuId = menuId; }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }

    [RelayCommand]
    public void ToggleShowNameEdit() => ShowNameEdit = !ShowNameEdit;

    [RelayCommand]
    public void ToggleShowCostEdit() => ShowCostEdit = !ShowCostEdit;
    public static AdminCateringMenuItemViewModel Get(CateringMenuItem model)
    {
        var menuId = _service.MenuCategories.FirstOrDefault(cat => cat.items.Any(it => it.id == model.id))?.id ?? string.Empty;

        return new AdminCateringMenuItemViewModel(menuId)
        {
            Item = CateringMenuItemViewModel.Get(model)
        };
    }
       

    [RelayCommand]
    public async Task Delete()
    {
        if (Item is null)
            return;
        if (Item.Deleted)
            return;
        Busy = true;
        BusyMessage = "Deleting item...";
        await _service.DeleteItem(Item.Id, OnError.DefaultBehavior(this));
        Item.Deleted = true;
        
        Busy = false;
        Deleted?.Invoke(this, this);
    }

    [RelayCommand]
    public async Task Save()
    {
        if (Item is null)
            return;
        if (Item.Deleted)
            return;
        Busy = true;
        BusyMessage = "Saving item...";

        var result = string.IsNullOrEmpty(Item.Id)
            ? await _service.CreateItem(
                MenuId, 
                new(
                    Item.Name, 
                    Item.PricePerPerson, 
                    Item.FieldTripItem,
                    DateTime.Today), 
                OnError.DefaultBehavior(this))
            : await _service.UpdateItem(
                Item.Id, 
                new(
                    Item.Name, 
                    Item.PricePerPerson, 
                    Item.FieldTripItem,
                    PriceChangeEffectiveDate), 
                OnError.DefaultBehavior(this));
        
        if (result is not null)
        {
            Item = CateringMenuItemViewModel.Get(result);
        }
        Busy = false;

        ShowNameEdit = false;
        ShowCostEdit = false;

        Saved?.Invoke(this, this);
    }
}