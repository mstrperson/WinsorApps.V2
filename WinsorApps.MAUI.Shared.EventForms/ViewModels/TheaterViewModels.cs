using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class TheaterEventViewModel :
    ObservableObject,
    IDefaultValueViewModel<TheaterEventViewModel>,
    IEventSubFormViewModel<TheaterEventViewModel, TheaterEvent>,
    IBusyViewModel,
    IErrorHandling,
    IModelCarrier<TheaterEventViewModel, TheaterEvent>
{
    private readonly EventFormsService _eventService = ServiceHelper.GetService<EventFormsService>();
    private readonly TheaterService _theater = ServiceHelper.GetService<TheaterService>();

    [ObservableProperty] private string id = "";
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private List<DocumentViewModel> documents = [];
    [ObservableProperty] private TheaterMenuCollectionViewModel theaterMenu = new();
    [ObservableProperty] private bool hasLoaded;

    public Optional<TheaterEvent> Model { get; private set; } = Optional<TheaterEvent>.None();

    public void Clear()
    {
        Id = "";
        Model = Optional<TheaterEvent>.None();
        Notes = "";
        Documents = [];
    }

    public void Load(TheaterEvent model)
    {
        if (model == default)
        {
            Clear();
            return;
        }

        Id = model.eventId;
        Notes = model.notes;
        Model = Optional < TheaterEvent > .Some(model);
        Documents = model.attachments.Select(doc => new DocumentViewModel(doc)).ToList();
        
        LoadSelections(model.items);
        HasLoaded = true;
    }

    public TheaterEventViewModel()
    {
        var task = _theater.WaitForInit(OnError.DefaultBehavior(this));
        task.WhenCompleted(() =>
        {
            TheaterMenu = new() { Menus = _theater.AvailableMenus.Select(TheaterMenuViewModel.Get).ToList() };
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

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "Working";
    public static TheaterEventViewModel Empty => new();


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
              .ToList()
        );

    [RelayCommand]
    public async Task Continue(bool template = false)
    {
        var result = await _eventService.PostTheaterDetails(Id, this, OnError.DefaultBehavior(this));
        if(result is not null)
        {
            Model = Optional < TheaterEvent > .Some(result);
            if(!template)
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

    private TheaterEventViewModel(TheaterEvent model) { Load(model); }

    public static TheaterEventViewModel Get(TheaterEvent model) => new(model);
}

public partial class TheaterMenuCollectionViewModel :
    ObservableObject
{
    [ObservableProperty] private List<TheaterMenuViewModel> menus = [];

    public TheaterMenuViewModel this[string id] => Menus.First(menu => menu.Id == id);
}

public partial class TheaterMenuViewModel :
    ObservableObject,
    ICachedViewModel<TheaterMenuViewModel, TheaterMenuCategory, TheaterService>,
    ISelectable<TheaterMenuViewModel>
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private List<TheaterMenuItemViewModel> items = [];
    [ObservableProperty] private bool isDeleted;
    [ObservableProperty] private bool isSelected;

    public TheaterMenuItemViewModel this[string id] => Items.First(it => it.Id == id);

    public static List<TheaterMenuViewModel> ViewModelCache { get; private set; } = [];

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
            Items = model.items.Select(TheaterMenuItemViewModel.Get).ToList()
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
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private bool isDeleted;
    [ObservableProperty] private bool isSelected;

    public event EventHandler<TheaterMenuItemViewModel>? Selected;

    public static TheaterMenuItemViewModel Get(TheaterMenuItem model) => new()
    {
        Id = model.id,
        Name = model.name,
        IsDeleted = model.deleted
    };

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}