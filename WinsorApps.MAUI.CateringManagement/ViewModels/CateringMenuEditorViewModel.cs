using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.CateringManagement.ViewModels;

public partial class CateringMenuEditorViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    IModelCarrier<CateringMenuEditorViewModel, CateringMenuCategory>
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private bool isDeleted;
    [ObservableProperty] private bool fieldTripCategory;
    [ObservableProperty] private ObservableCollection<CateringMenuItem> items = [];
    [ObservableProperty] private ObservableCollection<CateringMenuItem> availableItems = [];
    [ObservableProperty] private ObservableCollection<CateringMenuItem> fieldTripAvailableItems = [];
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    public Optional<CateringMenuCategory> Model { get; private set; } = Optional<CateringMenuCategory>.None();

    public event EventHandler<ErrorRecord>? OnError;

    public CateringMenuEditorViewModel() { }

    public CateringMenuEditorViewModel(CateringMenuCategory model)
    {
        Load(model);
    }

    public void Load(CateringMenuCategory model)
    {
        if (model == default)
        {
            Clear();
            return;
        }

        Id = model.id;
        Name = model.name;
        IsDeleted = model.isDeleted;
        FieldTripCategory = model.fieldTripCategory;
        Items = new(model.items);
        AvailableItems = new(model.AvailableItems ?? []);
        FieldTripAvailableItems = new(model.FieldTripAvailableItems ?? []);
        Model = Optional<CateringMenuCategory>.Some(model);
    }

    [RelayCommand]
    public void Clear()
    {
        Id = "";
        Name = "";
        IsDeleted = false;
        FieldTripCategory = false;
        Items = [];
        AvailableItems = [];
        FieldTripAvailableItems = [];
        Model = Optional<CateringMenuCategory>.None();
    }

    public static CateringMenuEditorViewModel Get(CateringMenuCategory model) => new(model);
}
