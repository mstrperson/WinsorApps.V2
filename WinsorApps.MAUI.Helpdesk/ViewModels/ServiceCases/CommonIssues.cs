using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;

public partial class CommonIssueViewModel : ObservableObject, ISelectable<CommonIssueViewModel>
{
    [ObservableProperty] string id = "";
    [ObservableProperty] string status = "";
    [ObservableProperty] string description = "";
    [ObservableProperty] bool isSelected;

    public event EventHandler<CommonIssueViewModel>? Selected;

    public CommonIssueViewModel(ServiceCaseCommonIssue issue)
    {
        id = issue.id;
        status = issue.status;
        description = issue.description;
    }

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

    [ObservableProperty] ImmutableArray<CommonIssueViewModel> items;

    public ImmutableArray<CommonIssueViewModel> Selected
    {
        get => [.. Items.Where(it => it.IsSelected)];
        set
        {
            foreach (var item in Items)
                item.IsSelected = value.Any(it => it.Id == item.Id);
        }
    }

    public void Select(ImmutableArray<string> commonIssues)
    {
        foreach (var item in Items)
            item.IsSelected = commonIssues.Any(ci => item.Status == ci);
    }
    public void Select(ImmutableArray<CommonIssueViewModel> commonIssues)
    {
        foreach (var item in Items)
            item.IsSelected = commonIssues.Any(ci => item.Id == ci.Id);
    }

    public CommonIssueSelectionViewModel()
    {
        items = _caseService.CommonIssues.Select(issue => new CommonIssueViewModel(issue)).ToImmutableArray();
    }
}