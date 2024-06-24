using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreKit;
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
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class TheaterEventViewModel :
    ObservableObject,
    IDefaultValueViewModel<CateringEventViewModel>,
    IEventSubFormViewModel,
    IBusyViewModel,
    IErrorHandling
{
    private readonly EventFormsService _eventService = ServiceHelper.GetService<EventFormsService>();
    private readonly TheaterService _theater = ServiceHelper.GetService<TheaterService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string notes = "";
    [ObservableProperty] ImmutableArray<DocumentViewModel> documents = [];
    [ObservableProperty] TheaterMenuCollectionViewModel theaterMenu = new();

    public static TheaterEventViewModel Get(TheaterEvent model)
    {
        var vm = new TheaterEventViewModel()
        {
            Id = model.eventId,
            Notes = model.notes,
            Documents = model.attachments.Select(doc => new DocumentViewModel(doc)).ToImmutableArray()
        };
        vm.LoadSelections(model.items);
        return vm;
    }

    public TheaterEventViewModel()
    {
        var task = _theater.WaitForInit(OnError.DefaultBehavior(this));
        task.WhenCompleted(() =>
        {
            TheaterMenu = new() { Menus = _theater.AvailableMenus.Select(TheaterMenuViewModel.Get).ToImmutableArray() };
        });
    }

    [RelayCommand]
    public void ClearSelections()
    {
        foreach (var menu in TheaterMenu.Menus)
            foreach (var item in menu.Items)
                item.IsSelected = false;
    }

    public void LoadSelections(IEnumerable<TheaterMenuItem> selectedItems)
    {
        ClearSelections();
        foreach (var item in selectedItems)
            TheaterMenu[item.categoryId][item.id].IsSelected = true;
    }

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";
    public static CateringEventViewModel Default => new();


    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    public static implicit operator NewTheaterEvent(TheaterEventViewModel vm) =>
        new(
            vm.Notes,
            vm.TheaterMenu
              .Menus
              .SelectMany(
                  menu => menu.Items
                      .Where(it => it.IsSelected)
                      .Select(it => it.Id))
              .ToImmutableArray()
        );

    [RelayCommand]
    public async Task Continue()
    {
        var result = await _eventService.PostTheaterDetails(Id, this, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            Id = result.Value.eventId;
            ReadyToContinue?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    public async Task Delete()
    {
        var success = await _eventService.DeleteTheaterDetails(Id, OnError.DefaultBehavior(this));
        if(success)
        {
            Deleted?.Invoke(this, EventArgs.Empty);
        }
    }
}

public partial class TheaterMenuCollectionViewModel :
    ObservableObject
{
    [ObservableProperty] ImmutableArray<TheaterMenuViewModel> menus = [];

    public TheaterMenuViewModel this[string id] => Menus.First(menu => menu.Id == id);
}

public partial class TheaterMenuViewModel :
    ObservableObject,
    ICachedViewModel<TheaterMenuViewModel, TheaterMenuCategory, TheaterService>,
    ISelectable<TheaterMenuViewModel>
{
    [ObservableProperty] string id = "";
    [ObservableProperty] string name = "";
    [ObservableProperty] ImmutableArray<TheaterMenuItemViewModel> items = [];
    [ObservableProperty] bool isDeleted;
    [ObservableProperty] bool isSelected;

    public TheaterMenuItemViewModel this[string id] => Items.First(it => it.Id == id);

    public static ConcurrentBag<TheaterMenuViewModel> ViewModelCache { get; private set; } = [];

    public event EventHandler<TheaterMenuViewModel>? Selected;

    public static TheaterMenuViewModel Get(TheaterMenuCategory model)
    {
        var vm = ViewModelCache.FirstOrDefault(menu => menu.Id == model.id);
        if (vm is not null)
            return vm.Clone();

        vm = new()
        {
            Id = model.id,
            Name = model.name,
            IsDeleted = model.deleted,
            Items = model.items.Select(TheaterMenuItemViewModel.Get).ToImmutableArray()
        };
        ViewModelCache.Add(vm);
        return vm.Clone(); 
    }

    public static List<TheaterMenuViewModel> GetClonedViewModels(IEnumerable<TheaterMenuCategory> models) => models.Select(Get).ToList();

    public static async Task Initialize(TheaterService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);
        _ = GetClonedViewModels(service.AvailableMenus);
    }

    public TheaterMenuViewModel Clone() => (TheaterMenuViewModel)MemberwiseClone();

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class TheaterMenuItemViewModel :
    ObservableObject,
    ISelectable<TheaterMenuItemViewModel>
{
    [ObservableProperty] string id = "";
    [ObservableProperty] string name = "";
    [ObservableProperty] bool isDeleted;
    [ObservableProperty] bool isSelected;

    public event EventHandler<TheaterMenuItemViewModel>? Selected;

    public static TheaterMenuItemViewModel Get(TheaterMenuItem model) => new()
    {
        Id = model.id,
        Name = model.name,
        IsDeleted = model.deleted
    };

    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}