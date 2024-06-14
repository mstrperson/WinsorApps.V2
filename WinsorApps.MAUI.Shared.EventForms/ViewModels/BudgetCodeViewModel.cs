using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class BudgetCodeViewModel : 
    ObservableObject, 
    IEmptyViewModel<BudgetCodeViewModel>,
    IErrorHandling,
    IAutoRefreshingCacheService,
    ICachedViewModel<BudgetCodeViewModel, BudgetCode, BudgetCodeService>
{
    private readonly BudgetCodeService _service = ServiceHelper.GetService<BudgetCodeService>();

    [ObservableProperty] private string codeId = "";
    [ObservableProperty] private string userId = "";
    [ObservableProperty] private string accountNumber = "";
    [ObservableProperty] private string commonName = "";

    public static ConcurrentBag<BudgetCodeViewModel> ViewModelCache { get; protected set; } = [];

    public TimeSpan RefreshInterval => TimeSpan.MaxValue;

    public bool Refreshing => false;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? OnCacheRefreshed;

    public static implicit operator BudgetCodeViewModel(BudgetCode budgetCode) => new()
    {
        CodeId = budgetCode.codeId,
        UserId = budgetCode.userId,
        AccountNumber = budgetCode.accountNumber,
        CommonName = budgetCode.name
    };

    [RelayCommand]
    public async Task Create()
    {
        var result = await _service.CreateNewBudgetCode(AccountNumber, CommonName, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            CodeId = result.Value.codeId;
            UserId = result.Value.userId;
            ViewModelCache.Add((BudgetCodeViewModel)result.Value);
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }
    }

    public static List<BudgetCodeViewModel> GetClonedViewModels(IEnumerable<BudgetCode> models)
    {
        List<BudgetCodeViewModel> result = [];
        foreach (var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(BudgetCodeService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);
        _ = GetClonedViewModels(service.BudgetCodes);
    }

    public static BudgetCodeViewModel Get(BudgetCode model)
    {
        var vm = ViewModelCache.FirstOrDefault(code => code.CodeId == model.codeId);
        if (vm is not null) return vm.Clone();
        vm = (BudgetCodeViewModel)model;
        ViewModelCache.Add(vm);
        return vm.Clone();
    }

    public BudgetCodeViewModel Clone() => (BudgetCodeViewModel)MemberwiseClone();

    public Task RefreshInBackground(CancellationToken token, ErrorAction onError) => Task.CompletedTask;
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
            available = BudgetCodeViewModel.GetClonedViewModels(_service.BudgetCodes).ToImmutableArray(); 
        
        foreach (var vm in Available)
            vm.OnError += (sender, err) => OnError?.Invoke(sender, err);
    }

    private void BudgetCodeCacheRefreshed(object? sender, EventArgs e)
    {
        Available = BudgetCodeViewModel.GetClonedViewModels(_service.BudgetCodes).ToImmutableArray();
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


