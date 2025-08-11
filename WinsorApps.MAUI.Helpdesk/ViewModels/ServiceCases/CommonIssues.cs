using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;

public partial class CommonIssueViewModel(ServiceCaseCommonIssue issue) : ObservableObject, ISelectable<CommonIssueViewModel>
{
    [ObservableProperty] private string id = issue.id;
    [ObservableProperty] private string status = issue.status;
    [ObservableProperty] private string description = issue.description;
    [ObservableProperty] private bool isSelected;

    public event EventHandler<CommonIssueViewModel>? Selected;

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
            Selected?.Invoke(this, this);
    }
}

public partial class CommonIssueSelectionViewModel : ObservableObject, ICheckBoxListViewModel<CommonIssueViewModel>
{
    private readonly ServiceCaseService _caseService = ServiceHelper.GetService<ServiceCaseService>();

    [ObservableProperty] private ObservableCollection<CommonIssueViewModel> items;

    public ObservableCollection<CommonIssueViewModel> Selected
    {
        get => [.. Items.Where(it => it.IsSelected)];
        set
        {
            foreach (var item in Items)
                item.IsSelected = value.Any(it => it.Id == item.Id);
        }
    }

    public void Select(List<string> commonIssues)
    {
        foreach (var item in Items)
            item.IsSelected = commonIssues.Any(ci => item.Status == ci);
    }
    public void Select(List<CommonIssueViewModel> commonIssues)
    {
        foreach (var item in Items)
            item.IsSelected = commonIssues.Any(ci => item.Id == ci.Id);
    }

    public CommonIssueSelectionViewModel()
    {
        items = [.._caseService.CommonIssues.Select(issue => new CommonIssueViewModel(issue))];
    }
}