using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class CateringEventViewModel :
    ObservableObject,
    IEmptyViewModel<CateringEventViewModel>,
    ICachedViewModel<CateringEventViewModel, CateringEvent, EventFormsService>
{
    private static readonly EventFormsService _eventsService = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] bool serversNeeded;
    [ObservableProperty] bool cleanupRequired;
    [ObservableProperty] double laborCost;
    [ObservableProperty] ImmutableArray<CateringMenuSelectionViewModel> selectedItems;
    [ObservableProperty] BudgetCodeViewModel budgetCode = IEmptyViewModel<BudgetCodeViewModel>.Empty;

    public static ConcurrentBag<CateringEventViewModel> ViewModelCache { get; protected set; } = [];

    public static CateringEventViewModel Get(CateringEvent model)
    {
        var vm = ViewModelCache.FirstOrDefault(evt => evt.Id == model.id);
        if (vm is not null) return vm.Clone();
        vm = new CateringEventViewModel()
        {
            Id = model.id,
            BudgetCode = BudgetCodeViewModel.Get(model.budgetCode),
            ServersNeeded = model.servers,
            CleanupRequired = model.cleanup,
            LaborCost = model.laborCost,
            SelectedItems = model.menuSelections.Select(detail => (CateringMenuSelectionViewModel)detail).ToImmutableArray()
        };
        ViewModelCache.Add(vm);

        return vm.Clone();
    }

    public static List<CateringEventViewModel> GetClonedViewModels(IEnumerable<CateringEvent> models)
    {
        List<CateringEventViewModel> result = [];
        foreach (var model in models)
        {
            result.Add(Get(model));
        }

        return result;
    }

    public static async Task Initialize(EventFormsService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);
        
        // TODO: Initiailze ViewModelCache once the service is done.
    }

    public CateringEventViewModel Clone() => (CateringEventViewModel)this.MemberwiseClone();
}

public partial class CateringMenuSelectionViewModel : 
    ObservableObject,
    IEmptyViewModel<CateringMenuSelectionViewModel>,
    ISelectable<CateringMenuSelectionViewModel>
{
    [ObservableProperty] CateringMenuItemViewModel item = IEmptyViewModel<CateringMenuItemViewModel>.Empty;
    [ObservableProperty] int quantity;
    [ObservableProperty] double cost;

    [ObservableProperty] bool isSelected;

    public event EventHandler<CateringMenuSelectionViewModel>? Selected;

    public void Select()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
            Selected?.Invoke(this, this);
    }

    public static CateringMenuSelectionViewModel Create(CateringMenuItemViewModel item) => new() { Item = item };  

    public static implicit operator CateringMenuSelectionViewModel(DetailedCateringMenuSelection detail) => new() 
    { 
        Cost = detail.cost, 
        Item = CateringMenuItemViewModel.Get(detail.item), 
        Quantity = detail.quantity 
    };

    public static implicit operator CateringMenuSelectionViewModel(CateringMenuSelection selection)
    {
        var item = CateringMenuItemViewModel.ViewModelCache.FirstOrDefault(item => item.Id == selection.itemId);
        if(item is null)
            return IEmptyViewModel<CateringMenuSelectionViewModel>.Empty;

        return new() { Item = item, Quantity = selection.quantity, Cost = selection.quantity * item.PricePerPerson };
    }
}

public partial class CateringMenuViewModel :
    ObservableObject,
    ICheckBoxListViewModel<CateringMenuSelectionViewModel>
{
    [ObservableProperty] string title = "";
    [ObservableProperty] ImmutableArray<CateringMenuSelectionViewModel> items = [];
    [ObservableProperty] bool isFieldTrip;
    [ObservableProperty] bool isDeleted;

    public static CateringMenuViewModel Create(CateringMenuCategory category) => new()
    {
        Title = category.name,
        Items = [..
            CateringMenuItemViewModel
            .GetClonedViewModels(category.items)
            .Select(CateringMenuSelectionViewModel.Create)
        ],
        IsFieldTrip = category.fieldTripCategory,
        IsDeleted = category.isDeleted
    };

    public void LoadSelections(IEnumerable<CateringMenuSelection> selections)
    {
        foreach(var sel in selections)
        {
            var vm = Items.FirstOrDefault(it => it.Item.Id == sel.itemId);
            if (vm is null)
                continue;

            vm.Quantity = sel.quantity;
            vm.Cost = sel.quantity * vm.Item.PricePerPerson;
        }
    }

    public void LoadSelections(IEnumerable<DetailedCateringMenuSelection> selections)
    {
        foreach (var sel in selections)
        {
            var vm = Items.FirstOrDefault(it => it.Item.Id == sel.item.id);
            if (vm is null)
                continue;

            vm.Quantity = sel.quantity;
            vm.Cost = sel.quantity * vm.Item.PricePerPerson;
        }
    }
}

public partial class CateringMenuCollectionViewModel :
    ObservableObject
{
    [ObservableProperty] ImmutableArray<CateringMenuViewModel> menus;
}

public partial class CateringMenuItemViewModel : 
    ObservableObject, 
    IEmptyViewModel<CateringMenuItemViewModel>, 
    ICachedViewModel<CateringMenuItemViewModel, CateringMenuItem, CateringMenuService>
{
    private readonly CateringMenuService _menuService = ServiceHelper.GetService<CateringMenuService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string name = "";
    [ObservableProperty] double pricePerPerson;
    [ObservableProperty] string category = "";
    [ObservableProperty] bool deleted;
    [ObservableProperty] bool fieldTripItem;
    [ObservableProperty] int ordinal = -1;

    public static ConcurrentBag<CateringMenuItemViewModel> ViewModelCache { get; protected set; } = [];

    public static CateringMenuItemViewModel Get(CateringMenuItem model)
    {
        var vm = ViewModelCache.FirstOrDefault(item => item.Id == model.id);
        if(vm is null)
        {
            vm = new()
            {
                Deleted = model.isDeleted,
                Id = model.id,
                Name = model.name,
                Category = model.category,
                FieldTripItem = model.fieldTripItem,
                Ordinal = model.ordinal,
                PricePerPerson = model.pricePerPerson
            };

            ViewModelCache.Add(vm);
        }

        return vm.Clone();
    }

    public static List<CateringMenuItemViewModel> GetClonedViewModels(IEnumerable<CateringMenuItem> models)
    {
        List<CateringMenuItemViewModel> result = [];

        foreach (var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(CateringMenuService service, ErrorAction onError) => await Task.CompletedTask;

    public CateringMenuItemViewModel Clone() => (CateringMenuItemViewModel)this.MemberwiseClone();
}

public partial class CateringMenuCategoryViewModel : 
    ObservableObject, 
    IEmptyViewModel<CateringMenuCategoryViewModel>, 
    ICachedViewModel<CateringMenuCategoryViewModel, CateringMenuCategory, CateringMenuService>
{
    private readonly CateringMenuService _menuService = ServiceHelper.GetService<CateringMenuService>();
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string name = "";
    [ObservableProperty] bool isDeleted;
    [ObservableProperty] bool fieldTripCategory;
    [ObservableProperty] ImmutableArray<CateringMenuItemViewModel> items;

    public CateringMenuCategoryViewModel()
    {
        _menuService.OnCacheRefreshed += (_, _) => Initialize(_menuService, _logging.LogError).SafeFireAndForget(e => e.LogException(_logging));
    }

    public static ConcurrentBag<CateringMenuCategoryViewModel> ViewModelCache { get; protected set; } = [];

    public static CateringMenuCategoryViewModel Get(CateringMenuCategory model)
    {
        var vm = ViewModelCache.FirstOrDefault(cat => cat.Id == model.id);
        if (vm is not null) return vm.Clone();
        vm = new() 
        { 
            Id = model.id, 
            FieldTripCategory = model.fieldTripCategory, 
            IsDeleted = model.isDeleted, 
            Name = model.name, 
            Items = model.items.Select(CateringMenuItemViewModel.Get).ToImmutableArray() 
        };

        ViewModelCache.Add(vm);

        return vm.Clone();
    }

    public static List<CateringMenuCategoryViewModel> GetClonedViewModels(IEnumerable<CateringMenuCategory> models)
    {
        List<CateringMenuCategoryViewModel> result = [];

        foreach (var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(CateringMenuService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

        _ = GetClonedViewModels(service.MenuCategories);
    }

    public CateringMenuCategoryViewModel Clone() => (CateringMenuCategoryViewModel)this.MemberwiseClone();
}