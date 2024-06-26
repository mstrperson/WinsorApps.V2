using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;

public partial class ServiceStatusViewModel : 
    ObservableObject, 
    ISelectable<ServiceStatusViewModel>,
    IDefaultValueViewModel<ServiceStatusViewModel>
{
    [ObservableProperty] string id = "";
    [ObservableProperty] string status = "";
    [ObservableProperty] string description = "";
    [ObservableProperty] string nextId = "";
    [ObservableProperty] ServiceStatusViewModel next = ServiceStatusViewModel.Default;
    [ObservableProperty] bool isSelected;

    public static ServiceStatusViewModel Default => new();

    public event EventHandler<ServiceStatusViewModel>? Selected;

    public void Select() => Selected?.Invoke(this, this);

    public ServiceStatusViewModel() { }

    public override string ToString() => Status;

}

public partial class ServiceStatusSearchViewModel : ObservableObject, ICachedSearchViewModel<ServiceStatusViewModel>
{
    private readonly ServiceCaseService _caseService = ServiceHelper.GetService<ServiceCaseService>();

    [ObservableProperty] private ImmutableArray<ServiceStatusViewModel> available;
    [ObservableProperty] private ImmutableArray<ServiceStatusViewModel> allSelected = [];
    [ObservableProperty] private ImmutableArray<ServiceStatusViewModel> options = [];
    [ObservableProperty] private ServiceStatusViewModel selected = ServiceStatusViewModel.Default;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty]
    private bool showOptions;
    [ObservableProperty]
    private string searchText = "";
    [ObservableProperty]
    private SelectionMode selectionMode = SelectionMode.Single;

    public event EventHandler<ServiceStatusViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults;
    public event EventHandler<ImmutableArray<ServiceStatusViewModel>>? OnMultipleResult;

    public ServiceStatusSearchViewModel()
    {
        Available = _caseService.ServiceStatuses
            .Select(status => new ServiceStatusViewModel()
            {
                Id = status.id,
                Description = status.description,
                IsSelected = false,
                Status = status.text,
                NextId = status.defaultNextId
            })
            .ToImmutableArray();

        foreach(var status in Available)
        {
            status.Next = Available.FirstOrDefault(st => st.Id == status.NextId) ?? ServiceStatusViewModel.Default;
            status.Selected += (_, st) => Select(st);
        }
    }

    [RelayCommand]
    public void Search()
    {
        var possible = Available
                .Where(status => status.Status.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase));
        if (!possible.Any())
            OnZeroResults?.Invoke(this, EventArgs.Empty);
        switch (SelectionMode)
        {
            case SelectionMode.Multiple:
                AllSelected = [.. possible];
                IsSelected = AllSelected.Length > 0;
                OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.Single:
                Options = [.. possible];
                if (Options.Length == 0)
                {
                    ShowOptions = false;
                    Selected = ServiceStatusViewModel.Default;
                    IsSelected = false;
                    return;
                }

                if (Options.Length == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    SearchText = Selected.Status;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = ServiceStatusViewModel.Default;
                IsSelected = false;
                return;
            default: return;
        }
    }

    public void Select(string str)
    {
        var status = Available.FirstOrDefault(st => st.Status.Equals(str, StringComparison.InvariantCultureIgnoreCase));
        if (status is not null)
            Select(status);
    }

    [RelayCommand]
    public void Select(ServiceStatusViewModel status)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                Selected = Available.FirstOrDefault(st => st.Id == status.Id) ?? ServiceStatusViewModel.Default;
                IsSelected = string.IsNullOrEmpty(Selected.Id);
                Options = [];
                ShowOptions = false;
                SearchText = Selected.Status;
                OnSingleResult?.Invoke(this, Selected);
                return;
            case SelectionMode.Multiple:
                var sta = Available.FirstOrDefault(st => st.Id == status.Id);
                if (sta is null) return;
                if (AllSelected.Contains(sta))
                    AllSelected = [.. AllSelected.Except([sta])];
                else
                    AllSelected = [.. AllSelected, sta];

                IsSelected = AllSelected.Length > 0;
                if(IsSelected)
                    OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    async Task IAsyncSearchViewModel<ServiceStatusViewModel>.Search() => await Task.Run(Search);
         
}