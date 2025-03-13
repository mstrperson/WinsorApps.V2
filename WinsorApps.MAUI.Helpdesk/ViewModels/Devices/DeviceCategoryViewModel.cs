using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels;

public partial class DeviceCategoryViewModel : ObservableObject, IDefaultValueViewModel<DeviceCategoryViewModel>
{
    private readonly DeviceCategoryRecord _category;

    public string Id => _category.id;

    public static DeviceCategoryViewModel Empty => new(DeviceCategoryRecord.Empty);

    [ObservableProperty] private string name = "";
    [ObservableProperty] private string prefix = "";
    [ObservableProperty] private bool assignCustody;
    [ObservableProperty] private string cheqroomCategory = "";
    [ObservableProperty] private string cheqroomLocation = "";
    [ObservableProperty] private string jamfDepartment = "";
    [ObservableProperty] private string jamfDeviceType = "";

    public DeviceCategoryViewModel() { _category = DeviceCategoryRecord.Empty; }

    public DeviceCategoryViewModel(DeviceCategoryRecord category)
    {
        _category = category;
        name = category.name;
        assignCustody = category.assignCustodyToOwner;
        prefix = category.assetTagPrefix;
        cheqroomCategory = category.cheqroomCategory;
        cheqroomLocation = category.cheqroomLocation;
        jamfDepartment = category.jamfDepartment;
        jamfDeviceType = category.jamfDeviceType;
    }

    public DeviceCategoryViewModel(string name)
    {
        var service = ServiceHelper.GetService<DeviceService>()!;
        var category =
            service.Categories.FirstOrNone(cat =>
                cat.name.Contains(name ?? "", StringComparison.InvariantCultureIgnoreCase))
            .Reduce(DeviceCategoryRecord.Empty);
        
        _category = category;
        this.name = category.name;
        assignCustody = category.assignCustodyToOwner;
        prefix = category.assetTagPrefix;
        cheqroomCategory = category.cheqroomCategory;
        cheqroomLocation = category.cheqroomLocation;
        jamfDepartment = category.jamfDepartment;
        jamfDeviceType = category.jamfDeviceType;
    }

    public override string ToString() => Name;
}

public partial class CategorySearchViewModel : 
    ObservableObject, 
    ICachedSearchViewModel<DeviceCategoryViewModel>, 
    IErrorHandling
{
    [ObservableProperty] private ObservableCollection<DeviceCategoryViewModel> available;
    [ObservableProperty] private ObservableCollection<DeviceCategoryViewModel> allSelected = [];
    [ObservableProperty] private SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private bool showOptions;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private ObservableCollection<DeviceCategoryViewModel> options = [];
    [ObservableProperty] private DeviceCategoryViewModel selected = DeviceCategoryViewModel.Empty;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<ObservableCollection<DeviceCategoryViewModel>>? OnMultipleResult;
    public event EventHandler<DeviceCategoryViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults;

    public CategorySearchViewModel()
    {
        var deviceService = ServiceHelper.GetService<DeviceService>();
        Available = [..deviceService.Categories.Select(cat => new DeviceCategoryViewModel(cat))];
    }

    public void Search()
    {
        var possible = Available
            .Where(cat => cat.Name.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase));
        if (!possible.Any())
            OnZeroResults?.Invoke(this, EventArgs.Empty);
        switch (SelectionMode)
        {
            case SelectionMode.Multiple:
                AllSelected = [.. possible];
                IsSelected = AllSelected.Count > 0;
                OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.Single:
                Options = [.. possible];
                if (Options.Count == 0)
                {
                    ShowOptions = false;
                    Selected = DeviceCategoryViewModel.Empty;
                    IsSelected = false;
                    OnZeroResults?.Invoke(this, EventArgs.Empty);
                    return;
                }

                if (Options.Count == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = DeviceCategoryViewModel.Empty;
                IsSelected = false;
                return;
            default: return;
        }
    }

    public void Select(DeviceCategoryViewModel item)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                Selected = Available.FirstOrDefault(user => user.Id == item.Id) ?? DeviceCategoryViewModel.Empty;
                IsSelected = string.IsNullOrEmpty(Selected.Id);
                Options = [];
                ShowOptions = false;
                SearchText = Selected.Name;
                OnSingleResult?.Invoke(this, Selected);
                return;
            case SelectionMode.Multiple:
                var cat = Available.FirstOrDefault(user => user.Id == item.Id);
                if (cat is null) return;
                if (AllSelected.Contains(cat))
                    AllSelected = [.. AllSelected.Except([cat])];
                else
                    AllSelected = [.. AllSelected, cat];

                IsSelected = AllSelected.Count > 0;
                if (IsSelected)
                    OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    async Task IAsyncSearchViewModel<DeviceCategoryViewModel>.Search() => await Task.Run(Search);
}