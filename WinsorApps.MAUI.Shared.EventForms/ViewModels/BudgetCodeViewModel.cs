using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class BudgetCodeViewModel : 
    ObservableObject, 
    IEmptyViewModel<BudgetCodeViewModel>,
    IErrorHandling
{
    private readonly BudgetCodeService _service = ServiceHelper.GetService<BudgetCodeService>();

    [ObservableProperty] private string codeId = "";
    [ObservableProperty] private string userId = "";
    [ObservableProperty] private string accountNumber = "";
    [ObservableProperty] private string commonName = "";

    public event EventHandler<ErrorRecord>? OnError;

    public BudgetCodeViewModel() { }

    public BudgetCodeViewModel(BudgetCode budgetCode)
    {
        codeId = budgetCode.codeId;
        userId = budgetCode.userId;
        accountNumber = budgetCode.accountNumber;
    }

    [RelayCommand]
    public async Task Create()
    {
        var result = await _service.CreateNewBudgetCode(AccountNumber, CommonName, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            CodeId = result.Value.codeId;
            UserId = result.Value.userId;
        }
    }
}

public partial class BudgetCodeSearchViewModel :
    ObservableObject,
    ICachedSearchViewModel<BudgetCodeViewModel>,
    IErrorHandling
{
    private readonly BudgetCodeService _service;

    [ObservableProperty]
    private ImmutableArray<BudgetCodeViewModel> available = [];
    [ObservableProperty]
    private ImmutableArray<BudgetCodeViewModel> allSelected  = [];
    [ObservableProperty]
    private ImmutableArray<BudgetCodeViewModel> options  = [];
    [ObservableProperty]
    private BudgetCodeViewModel selected = IEmptyViewModel<BudgetCodeViewModel>.Empty;
    [ObservableProperty]
    private SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty]
    private string searchText = "";
    [ObservableProperty]
    private bool isSelected;
    [ObservableProperty]
    private bool showOptions;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<ImmutableArray<BudgetCodeViewModel>>? OnMultipleResult;
    public event EventHandler<BudgetCodeViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults;

    public BudgetCodeSearchViewModel(BudgetCodeService service)
    {
        _service = service;
        _service.OnCacheRefreshed += BudgetCodeCacheRefreshed;
        if (_service.Ready)
            available = _service.BudgetCodes.Select(code => new BudgetCodeViewModel(code)).ToImmutableArray();
    }

    private void BudgetCodeCacheRefreshed(object? sender, EventArgs e)
    {
        Available = _service.BudgetCodes.Select(code => new BudgetCodeViewModel(code)).ToImmutableArray();
        foreach (var vm in Available)
            vm.OnError += (sender, err) => OnError?.Invoke(sender, err);
    }

    [RelayCommand]
    public void Search()
    {
        var possible = Available.Where(code =>
            code.CommonName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase) || 
            code.AccountNumber.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase))
            .ToImmutableArray();

        switch(SelectionMode)
        {
            case SelectionMode.Single:
                
                if(possible.Length == 1)
                {
                    Selected = possible[0];
                    IsSelected = true;
                    ShowOptions = false;
                    Options = [];
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                Selected = IEmptyViewModel<BudgetCodeViewModel>.Empty;
                IsSelected = false;

                if (possible.Length == 0)
                {
                    Options = [];
                    ShowOptions = false;
                    OnZeroResults?.Invoke(this, EventArgs.Empty);
                    return;
                }

                Options = possible;
                ShowOptions = true;
                return;
            case SelectionMode.Multiple:
                if(possible.Length == 0)
                {
                    OnZeroResults?.Invoke(this, EventArgs.Empty);
                    return;
                }
                AllSelected = possible;
                OnMultipleResult?.Invoke(this, AllSelected);
                return;

            case SelectionMode.None:
            default:
                return;
        }
    }

    [RelayCommand]
    public void Select(BudgetCodeViewModel item)
    {
        var sel = Available.FirstOrDefault(c => c.CodeId == item.CodeId);
        if(sel is null)
        {
            OnError?.Invoke(this, new("Invalid Selection", $"Budget Code {item.CommonName} - {item.AccountNumber} ({item.CodeId}) does not appear in the Available collection."));
            return;
        }

        switch(SelectionMode)
        {
            case SelectionMode.Single:
                Selected = sel;
                IsSelected = true; 
                ShowOptions = false;
                Options = [];
                OnSingleResult?.Invoke(this, Selected);
                return;

            case SelectionMode.Multiple:
                if (AllSelected.Contains(sel))
                    AllSelected = AllSelected.Remove(sel);
                else
                    AllSelected = AllSelected.Add(sel);
                return;

            case SelectionMode.None:
            default:
                return;
        }
    }

    async Task IAsyncSearchViewModel<BudgetCodeViewModel>.Search()
    {
        await Task.Run(Search);
    }
}


