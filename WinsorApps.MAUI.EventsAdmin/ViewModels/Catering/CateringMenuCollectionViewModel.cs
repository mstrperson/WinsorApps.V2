
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels.Catering;

public partial class CateringMenuCollectionViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly CateringMenuService _service = ServiceHelper.GetService<CateringMenuService>();

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] ObservableCollection<AdminCateringMenuViewModel> allMenus = [];
    [ObservableProperty] ObservableCollection<AdminCateringMenuViewModel> visibleMenus = [];
    [ObservableProperty] bool showDeleted;


    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AdminCateringMenuViewModel>? MenuSelected;

    public CateringMenuCollectionViewModel(IEnumerable<CateringMenuCategory> models)
    {
        AllMenus = new ObservableCollection<AdminCateringMenuViewModel>(
            models.Select(AdminCateringMenuViewModel.Get));
        foreach (var menu in AllMenus)
        {
            menu.Selected += (_, it) => MenuSelected?.Invoke(this, it);
            menu.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            menu.Deleted += (_, it) =>
            {
                menu.IsDeleted = true;
                VisibleMenus = ShowDeleted
                    ? [.. AllMenus]
                    : [.. AllMenus.Where(m => !m.IsDeleted) ];
            };
            menu.OnError += (sender, error) => OnError?.Invoke(sender, error);
            menu.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }
        VisibleMenus = [.. AllMenus.Where(menu => !menu.IsDeleted)];
    }

    [RelayCommand]
    public void ToggleShowDeleted()
    {
        ShowDeleted = !ShowDeleted;
        
        VisibleMenus = ShowDeleted ? [.. AllMenus] : [.. AllMenus.Where(m => !m.IsDeleted)];
    }
}

public partial class AdminCateringMenuViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    ISelectable<AdminCateringMenuViewModel>
{
    private static readonly CateringMenuService _service = ServiceHelper.GetService<CateringMenuService>();

    [ObservableProperty] string name;
    [ObservableProperty] string id = "";
    [ObservableProperty] bool isDeleted;
    [ObservableProperty] bool fieldTripCategory;
    [ObservableProperty] CateringItemCollectionViewModel items = new CateringItemCollectionViewModel([]);
    [ObservableProperty] AdminCateringMenuItemViewModel newItem = new("");
    [ObservableProperty] bool showNewItem = false;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] bool isNew = true;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AdminCateringMenuViewModel>? Selected;
    public event EventHandler<AdminCateringMenuViewModel>? Created;
    public event EventHandler<AdminCateringMenuViewModel>? Deleted;

    [RelayCommand]
    public void AddNewItem(DateTime effectiveDate = default)
    {
        if(effectiveDate == default)
            effectiveDate = DateTime.Today;
        ShowNewItem = true;
        NewItem = new AdminCateringMenuItemViewModel(Id) { PriceChangeEffectiveDate = effectiveDate };
        NewItem.OnError += (sender, error) => OnError?.Invoke(this, error);
        NewItem.Saved += (_, item) =>
        {
            if (item.Item is not null && Items.AllItems.All(it => it.Item.Id != item.Item.Id))
            {
                Items.AllItems.Add(item);
                if(!item.Item.Deleted || Items.ShowDeleted)
                    Items.VisibleItems.Add(item);
                ShowNewItem = false;
            }
        };
    }

    public async Task MoveItemOrdinal(string itemId, int ordinal)
    {
        Busy = true;
        BusyMessage = "Moving item...";
        var menu = await _service.MoveItemOrdinal(Id, itemId, ordinal, OnError.DefaultBehavior(this));
        if (menu is not null)
            ReloadItemList(menu);

        Busy = false;
    }

    private void ReloadItemList(CateringMenuCategory menu)
    {
        var pairs = Items.AllItems.OrderBy(it => it.Item.Id)
                    .Zip(menu.items.OrderBy(it => it.id));

        foreach (var (oldItem, newItem) in pairs)
        {
            oldItem.Item.Ordinal = newItem.ordinal;
        }

        Items.AllItems = [.. Items.AllItems.OrderBy(it => it.Item.Ordinal)];
        Items.VisibleItems = Items.ShowDeleted
            ? [.. Items.AllItems]
            : [.. Items.AllItems.Where(m => !m.Item.Deleted).OrderBy(it => it.Item.Ordinal)];
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }

    public static AdminCateringMenuViewModel Get(CateringMenuCategory model)
    {
        var vm = new AdminCateringMenuViewModel()
        {
            Id = model.id,
            Name = model.name,
            IsDeleted = model.isDeleted,
            FieldTripCategory = model.fieldTripCategory,
            Items = new CateringItemCollectionViewModel(model.items),
            NewItem = new AdminCateringMenuItemViewModel(model.id),
            IsNew = false,
        };

        vm.Items.OnError += (sender, error) => vm.OnError?.Invoke(sender, error);
        vm.Items.PropertyChanged += ((IBusyViewModel)vm).BusyChangedCascade;

        vm.Items.ItemDeleted += async (_, item) =>
        {
            vm.Busy = true;
            vm.BusyMessage = "Reloaing Items.";
            await _service.RefreshCache();
            var menu = _service.MenuCategories.FirstOrDefault(cat => cat.id == model.id);
            if(menu is not null)
                vm.ReloadItemList(menu);
            vm.Busy = false;
        };

        return vm;
    }
    [RelayCommand]
    public async Task Delete()
    {
        if (string.IsNullOrEmpty(Id) || IsDeleted)
            return;
        Busy = true;
        BusyMessage = "Deleting menu...";
        await _service.DeleteCategory(Id, OnError.DefaultBehavior(this));
        IsDeleted = true;
        Busy = false;
        Deleted?.Invoke(this, this);
    }

    [RelayCommand]
    public async Task Save()
    {
        if (IsDeleted || !IsNew)
            return;
        Busy = true;
        BusyMessage = "Saving menu...";

        CateringMenuCategory? result;
        if (string.IsNullOrEmpty(Id))
        {
            result = await _service.CreateCategory(
                new CreateCateringMenuCategory(Name, FieldTripCategory),
                OnError.DefaultBehavior(this));
        }
        else
        {
            // No update method for category in CateringMenuService, so just refresh cache or handle as needed.
            result = null;
        }

        if (result is not null)
        {
            Id = result.id;
        }
        Busy = false;
        IsNew = false;
        Created?.Invoke(this, this);
    }
}