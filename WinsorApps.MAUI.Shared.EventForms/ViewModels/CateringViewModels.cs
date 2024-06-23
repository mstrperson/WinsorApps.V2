using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class CateringEventViewModel :
    ObservableObject,
    IEmptyViewModel<CateringEventViewModel>,
    ICachedViewModel<CateringEventViewModel, CateringEvent, EventFormsService>,
    IEventSubFormViewModel,
    IBusyViewModel,
    IErrorHandling
{
    private static readonly EventFormsService _eventsService = ServiceHelper.GetService<EventFormsService>();
    private static readonly CateringMenuService _cateringService = ServiceHelper.GetService<CateringMenuService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] bool serversNeeded;
    [ObservableProperty] bool cleanupRequired;
    [ObservableProperty] double laborCost;
    [ObservableProperty] CateringMenuCollectionViewModel menu = new(_cateringService);
    [ObservableProperty] BudgetCodeSearchViewModel budgetCodeSearch = new();
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";

    public static ConcurrentBag<CateringEventViewModel> ViewModelCache { get; protected set; } = [];


    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;

    public static CateringEventViewModel Get(CateringEvent model)
    {
        var vm = ViewModelCache.FirstOrDefault(evt => evt.Id == model.id);
        if (vm is not null) return vm.Clone();
        vm = new CateringEventViewModel()
        {
            Id = model.id,
            ServersNeeded = model.servers,
            CleanupRequired = model.cleanup,
            LaborCost = model.laborCost
        };

        vm.BudgetCodeSearch.Select(BudgetCodeViewModel.Get(model.budgetCode));
        vm.Menu.LoadMenuSelections(model.menuSelections);
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

    [RelayCommand]
    public async Task Continue()
    {
        Busy = true;
        var details = new NewCateringEvent(
            ServersNeeded, 
            CleanupRequired,
            Menu.Menus.SelectMany(menu =>
                menu.Items
                    .Where(sel => sel.IsSelected)
                    .Select(selection =>
                        new CateringMenuSelection(selection.Item.Id, selection.Quantity)))
            .ToImmutableArray(),
            BudgetCodeSearch.Selected.CodeId);
        var updated = await _eventsService.PostCateringDetails(Id, details, OnError.DefaultBehavior(this));
        if(updated.HasValue)
        {
            ReadyToContinue?.Invoke(this, EventArgs.Empty);
        }
        Busy = false;
    }

    [RelayCommand]
    public async Task Delete()
    {
        Busy = true;

        await _eventsService.DeleteCateringDetails(Id, OnError.DefaultBehavior(this));

        Busy = false;

        Deleted?.Invoke(this, EventArgs.Empty);
    }
}

public partial class CateringMenuSelectionViewModel : 
    ObservableObject,
    IEmptyViewModel<CateringMenuSelectionViewModel>,
    ISelectable<CateringMenuSelectionViewModel>,
    IErrorHandling
{
    [ObservableProperty] CateringMenuItemViewModel item = IEmptyViewModel<CateringMenuItemViewModel>.Empty;
    [ObservableProperty] int quantity;
    [ObservableProperty] double cost;

    [ObservableProperty] bool isSelected;

    public event EventHandler<CateringMenuSelectionViewModel>? Selected;
    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
            Selected?.Invoke(this, this);
    }

    [RelayCommand]
    public void Clear()
    {
        Quantity = 0;
        Cost = 0;
        IsSelected = false;
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
    ICheckBoxListViewModel<CateringMenuSelectionViewModel>,
    ISelectable<CateringMenuViewModel>,
    IErrorHandling
{
    [ObservableProperty] string id = "";
    [ObservableProperty] string title = "";
    [ObservableProperty] ImmutableArray<CateringMenuSelectionViewModel> items = [];
    [ObservableProperty] bool isFieldTrip;
    [ObservableProperty] bool isDeleted;
    [ObservableProperty] bool isSelected;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<CateringMenuViewModel>? Selected;

    public static CateringMenuViewModel Create(CateringMenuCategory category)
    {
        var vm = new CateringMenuViewModel()
        {
            Id = category.id,
            Title = category.name,
            Items = [..
            CateringMenuItemViewModel
                .GetClonedViewModels(category.items)
                .Select(CateringMenuSelectionViewModel.Create)
            ],
            IsFieldTrip = category.fieldTripCategory,
            IsDeleted = category.isDeleted
        };

        foreach(var item in vm.Items)
        {
            item.OnError += (sender, e) => vm.OnError?.Invoke(sender, e);
        }

        return vm;
    }

    [RelayCommand]
    public void ClearSelections()
    {
        foreach(var selection in Items)
        {
            selection.Clear();
        }
    }

    public void LoadSelections(IEnumerable<CateringMenuSelection> selections)
    {
        ClearSelections();
        foreach (var sel in selections)
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
        ClearSelections();
        foreach (var sel in selections)
        {
            var vm = Items.FirstOrDefault(it => it.Item.Id == sel.item.id);
            if (vm is null)
                continue;
            vm.Quantity = sel.quantity;
            vm.Cost = sel.quantity * vm.Item.PricePerPerson;
        }
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
            Selected?.Invoke(this, this);
    }
}

public partial class CateringMenuCollectionViewModel :
    ObservableObject,
    IErrorHandling
{
    private readonly CateringMenuService _service;
    [ObservableProperty] ImmutableArray<CateringMenuViewModel> menus;
    [ObservableProperty] CateringMenuViewModel selectedMenu = IEmptyViewModel<CateringMenuViewModel>.Empty;
    
    public CateringMenuViewModel this[string menuId]
    {
        get
        {
            var menu = Menus.FirstOrDefault(m => m.Id == menuId);
            if (menu is null)
                throw new ArgumentException(nameof(menuId), $"{menuId} is not valid.");

            return menu;
        }
    }

    public CateringMenuSelectionViewModel this[CateringMenuItem item]
    {
        get
        {
            foreach(var menu in Menus)
            {
                if (menu.Items.Any(sel => sel.Item.Id == item.id))
                    return menu.Items.First(sel => sel.Item.Id == item.id);
            }

            throw new ArgumentException(nameof(item), $"Item {item.id} not found");
        }
    }

    public CateringMenuCollectionViewModel(CateringMenuService service)
    {
        _service = service;
        var task = _service.WaitForInit(OnError.DefaultBehavior(this));
        task.WhenCompleted(() =>
        {
            Menus = _service.MenuCategories.Select(cat => CateringMenuViewModel.Create(cat)).ToImmutableArray();
            foreach (var menu in Menus)
            {
                menu.OnError += (sender, e) => OnError?.Invoke(sender, e);
                menu.Selected += (sender, e) => SelectedMenu = e;
            }
        });
    }

    [RelayCommand]
    public void ClearSelections()
    {
        foreach(var menu in Menus)
        {
            menu.ClearSelections();
        }
    }

    public void LoadMenuSelections(ImmutableArray<DetailedCateringMenuSelection> selections)
    {
        ClearSelections();
        foreach (var sel in selections)
        {
            var vm = this[sel.item];
            vm.Quantity = sel.quantity;
            vm.Cost = sel.cost;
            vm.IsSelected = true;
        }
    }

    public event EventHandler<ErrorRecord>? OnError;
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