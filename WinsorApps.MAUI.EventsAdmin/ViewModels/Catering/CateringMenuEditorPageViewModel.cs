using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels.Catering;

public partial class CateringMenuEditorPageViewModel :
    ObservableObject,
    IBusyViewModel,
    IErrorHandling,
    IAsyncInitService
{
    private readonly CateringMenuService _service = ServiceHelper.GetService<CateringMenuService>();

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] CateringMenuCollectionViewModel allMenus = new([]);
    [ObservableProperty] AdminCateringMenuViewModel selectedMenu = new();
    [ObservableProperty] bool showSelectedMenu;
    [ObservableProperty] DateTime priceChangeEffectiveDate = DateTime.Today;

    [RelayCommand]
    public void ShowNewMenu()
    {
        SelectedMenu = new();
        SelectedMenu.Created += (_, menu) =>
        {
            AllMenus.AllMenus.Add(menu);
            AllMenus.VisibleMenus.Add(menu);
            SelectedMenu = menu;
            ShowSelectedMenu = true;
            menu.Deleted += (_, menu) =>
            {
                AllMenus.AllMenus.Remove(menu);
                AllMenus.VisibleMenus.Remove(menu);
                SelectedMenu = new();
                ShowSelectedMenu = false;
            };
        };
        ShowSelectedMenu = true;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Refreshing Menu Cache";
        await _service.RefreshCache();

        Busy = false;
    }

    public CateringMenuEditorPageViewModel()
    {
        _service.OnCacheRefreshed += (_, _) =>
        {
            AllMenus = new CateringMenuCollectionViewModel(_service.MenuCategories);
            AllMenus.MenuSelected += (_, menu) =>
            {
                SelectedMenu = menu;
                ShowSelectedMenu = true;
            };
            AllMenus.OnError += (sender, error) => OnError?.Invoke(sender, error);
            AllMenus.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        };
    }


    public bool Started { get; private set; }
    public bool Ready { get; private set; }
    public double Progress { get; private set; }
    public string CacheFileName => _service.CacheFileName;

    public void ClearCache() => _service.ClearCache();
    public async Task SaveCache() => await _service.SaveCache();
    public bool LoadCache() => _service.LoadCache();

    public async Task Initialize(ErrorAction onError)
    {
        if (Started)
            return;

        Busy = true;
        BusyMessage = "Loading catering allMenus...";
        Started = true;
        Progress = 0;

        await _service.Initialize(onError);
        Progress = 0.5;

        var categories = _service.MenuCategories;
        AllMenus = new CateringMenuCollectionViewModel(categories);
        AllMenus.MenuSelected += (_, menu) =>
        {
            SelectedMenu = menu;
            ShowSelectedMenu = true;
        };
        Progress = 1;
        Ready = true;
        Busy = false;
        BusyMessage = "";
    }

    public async Task WaitForInit(ErrorAction onError) => await _service.WaitForInit(onError);

    public async Task Refresh(ErrorAction onError)
    {
        Busy = true;
        BusyMessage = "Refreshing catering allMenus...";
        await _service.Refresh(onError);
        AllMenus = new CateringMenuCollectionViewModel(_service.MenuCategories);
        AllMenus.MenuSelected += (_, menu) =>
        {
            SelectedMenu = menu;
            ShowSelectedMenu = true;
        };
        Busy = false;
        BusyMessage = "";
    }
}
