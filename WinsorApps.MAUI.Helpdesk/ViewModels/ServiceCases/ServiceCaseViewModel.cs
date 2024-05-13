using System.Collections.Immutable;
using System.Linq;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;

public partial class ServiceCaseViewModel : 
    ObservableObject, 
    IEmptyViewModel<ServiceCaseViewModel>, 
    ISelectable<ServiceCaseViewModel>,
    IErrorHandling,
    IAsyncSubmit
{
    private readonly ServiceCaseService _caseService = ServiceHelper.GetService<ServiceCaseService>();
    private readonly DeviceService _deviceService = ServiceHelper.GetService<DeviceService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<ServiceCaseViewModel>? Selected;

    public event EventHandler<ServiceCaseViewModel>? OnClose;
    public event EventHandler<ServiceCaseViewModel>? OnUpdate;
    public event EventHandler<ServiceCaseViewModel>? OnCreate;

    [ObservableProperty] string summaryText = "New Service Case";

    [RelayCommand]
    public void Select() => Selected?.Invoke(this, this);

    private ServiceCase _case;
    public ServiceCaseViewModel()
    {
        _case = new();
        loanerSearch = new()
        {
            Available = _deviceService.Loaners
                .Select(dev => new DeviceViewModel(dev))
                .ToImmutableArray()
        };

        StatusSearch.Select("Intake");
    }

    public ServiceCaseViewModel(ServiceCase serviceCase)
    {
        LoanerSearch = new()
        {
            Available = _deviceService.Loaners
                .Select(dev => new DeviceViewModel(dev))
                .ToImmutableArray()
        };

        LoadServiceCase(serviceCase);
    }

    private void LoadServiceCase(ServiceCase serviceCase)
    {
        _case = serviceCase;
        Id = serviceCase.id;
        LoanerSearch = new()
        {
            Available = _deviceService.Loaners
                .Select(dev => new DeviceViewModel(dev))
                .ToImmutableArray()
        };
        LoanerSearch.Select(LoanerSearch.Available.FirstOrDefault(dev => dev.WinsorDevice.AssetTag == serviceCase.loaner) ?? IEmptyViewModel<DeviceViewModel>.Empty);
        OwnerSearch.Select(OwnerSearch.Available.FirstOrDefault(u => u.Id == serviceCase.owner.id) ?? IEmptyViewModel<UserViewModel>.Empty);
        DeviceSearch.Select(DeviceSearch.Available.FirstOrDefault(dev => dev.Id == serviceCase.device.id) ?? IEmptyViewModel<DeviceViewModel>.Empty);
        CommonIssues.Select(serviceCase.commonIssues);
        IntakeNotes = serviceCase.intakeNotes;
        Opened = serviceCase.opened;
        IsClosed = serviceCase.closed.HasValue;
        Closed = IsClosed ? serviceCase.closed!.Value : DateTime.MinValue;
        StatusSearch.Select(serviceCase.status);
        AttachedDocuments = serviceCase.attachedDocuments.Select(doc => new DocumentViewModel(doc)).ToImmutableArray();
        RepairCost = serviceCase.repairCost;

        SummaryText = $"[{Status.Status}] {Device.DisplayName} - {Opened:dd MMM yyyy}";
    }

    [ObservableProperty] private string id = "";

    [ObservableProperty] private DeviceSearchViewModel deviceSearch = new();

    [ObservableProperty] private UserSearchViewModel ownerSearch = new();

    [ObservableProperty] private CommonIssueSelectionViewModel commonIssues = new();
    [ObservableProperty] private string intakeNotes = "";
    [ObservableProperty] private DateTime opened = DateTime.Now;
    [ObservableProperty] private DateTime closed = DateTime.MinValue;
    [ObservableProperty] private bool isClosed;
    [ObservableProperty] private ServiceStatusSearchViewModel statusSearch = new();
    [ObservableProperty] private ImmutableArray<DocumentViewModel> attachedDocuments = [];
    [ObservableProperty] private double repairCost = 0;
    [ObservableProperty] private DeviceSearchViewModel loanerSearch = new();
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool working;

    public DeviceViewModel Device
    {
        get => DeviceSearch.Selected;
        set => DeviceSearch.Select(value);
    }
    public DeviceViewModel Loaner
    {
        get => LoanerSearch.Selected;
        set => LoanerSearch.Select(value);
    }
   
    public UserViewModel Owner
    {
        get => OwnerSearch.Selected;
        set => OwnerSearch.Select(value);
    }
    public ServiceStatusViewModel Status
    {
        get => StatusSearch.Selected;
        set => StatusSearch.Select(value);
    }
    public ImmutableArray<CommonIssueViewModel> CommonIssueList
    {
        get => CommonIssues.Selected;
        set => CommonIssues.Select(value);
    }

    [RelayCommand]
    public async Task Submit()
    {
        Working = true;
        if (!string.IsNullOrEmpty(Id))
        {
            var result = await _caseService.UpdateServiceCase(new(Id, Status.Id, IntakeNotes, [..CommonIssueList.Select(issue => issue.Id)]), OnError.DefaultBehavior(this));
            Working = result.HasValue;
            if (!result.HasValue)
                return;

            LoadServiceCase(result.Value);
            Working = false;
            OnUpdate?.Invoke(this, this);
            return;
        }
        var newResult = await _caseService.OpenNewServiceCaseAsync(new(Device.Id, [..CommonIssueList.Select(issue => issue.Id)], IntakeNotes, Status.Id), OnError.DefaultBehavior(this));
        Working = newResult.HasValue;
        if(!newResult.HasValue) return;

        LoadServiceCase(newResult.Value);
        Working = false;
        OnCreate?.Invoke(this, this);
    }

    [RelayCommand]
    public async Task IncrementStatus()
    {
        if (Status.NextId == Status.Id)
            return;
        Working = true;
        var success = await _caseService.IncrementCaseStatus(Id, OnError.DefaultBehavior(this));
        if (success)
        {
            Status = Status.Next;
            OnUpdate?.Invoke(this, this);
        }
        Working = false;
    }

    [RelayCommand]
    public async Task SendNotification()
    {
        Working = true;
        await _caseService.SendPickupNotification(Id, OnError.DefaultBehavior(this));
        Working = false;
    }

    [RelayCommand]
    public async Task Close()
    {
        Working = true;
        var result = await _caseService.CloseServiceCase(Id, OnError.DefaultBehavior(this));
        if (result)
            OnClose?.Invoke(this, this);
        Working = false;
    }
}

public partial class ServiceCaseSearchViewModel : ObservableObject, ICachedSearchViewModel<ServiceCaseViewModel>, IErrorHandling, IMultiModalSearch<ServiceCaseViewModel>
{
    private readonly ServiceCaseService _caseService = ServiceHelper.GetService<ServiceCaseService>();

    [ObservableProperty] private ImmutableArray<ServiceCaseViewModel> available;
    [ObservableProperty] private ImmutableArray<ServiceCaseViewModel> allSelected = [];
    [ObservableProperty] private ImmutableArray<ServiceCaseViewModel> options = [];
    [ObservableProperty] private ServiceCaseViewModel selected = IEmptyViewModel<ServiceCaseViewModel>.Empty;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool showOptions;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty] private string searchMode;
    [ObservableProperty] private ServiceCaseFilterViewModel filter = new();

    public ServiceCaseSearchViewModel()
    {
        available = _caseService.OpenCases.Select(c => new ServiceCaseViewModel(c)).ToImmutableArray();
        foreach(var serviceCase in Available)
        {
            serviceCase.OnError += OnError.PassAlong();
            serviceCase.Selected += (_, sc) => Select(sc);
        }
        searchMode = SearchModes[0];
    }

    public event EventHandler<ServiceCaseViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults; 
    public event EventHandler<ImmutableArray<ServiceCaseViewModel>>? OnMultipleResult;
    public event EventHandler<ErrorRecord>? OnError;

    public event EventHandler<ServiceCaseViewModel>? OnCaseClosed;
    public event EventHandler<ServiceCaseViewModel>? OnCaseUpdated;
    public event EventHandler<ServiceCaseViewModel>? OnCaseCreated;

    public ImmutableArray<string> SearchModes => ["By Device", "By User", "By Status", "By Common Issue"];

    public Func<ServiceCaseViewModel, bool> SearchFilter =>
        SearchMode switch
        {
            "By Device" => (ServiceCaseViewModel serviceCase) =>
                serviceCase.Device.SerialNumber.Equals(SearchText, StringComparison.InvariantCultureIgnoreCase) ||
                serviceCase.Device.WinsorDevice.AssetTag.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase),
            "By User" => (ServiceCaseViewModel serviceCase) =>
                serviceCase.Owner.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase),
            "By Status" => (ServiceCaseViewModel serviceCase) =>
                serviceCase.Status.Status.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase),
            "By Common Issue" => (ServiceCaseViewModel serviceCase) =>
                serviceCase.CommonIssueList.Any(issue => issue.IsSelected && issue.Status.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)),
            _ => sc => true
        };

    [RelayCommand]
    public async Task Refresh()
    {
        await _caseService.Refresh(OnError.DefaultBehavior(this)); 
        
        Available = _caseService.OpenCases.Select(c => new ServiceCaseViewModel(c)).ToImmutableArray();
        foreach (var serviceCase in Available)
        {
            serviceCase.OnError += OnError.PassAlong();
            serviceCase.Selected += (_, sc) => Select(sc);
        }
    }

    [RelayCommand]
    public void Select(ServiceCaseViewModel serviceCase)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                Selected = Available.FirstOrDefault(sc => sc.Id == serviceCase.Id) ?? serviceCase;
                IsSelected = string.IsNullOrEmpty(Selected.Id);
                Options = [];
                ShowOptions = false;
                OnSingleResult?.Invoke(this, Selected);
                return;
            case SelectionMode.Multiple:
                var user = Available.FirstOrDefault(user => user.Id == serviceCase.Id);
                if (user is null) return;
                if (AllSelected.Contains(user))
                    AllSelected = [.. AllSelected.Except([user])];
                else
                    AllSelected = [.. AllSelected, user];

                IsSelected = AllSelected.Length > 0;
                if (IsSelected)
                    OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    [RelayCommand]
    public void Search()
    {
        var possible = Available
           .Where(SearchFilter);
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
                    Selected = IEmptyViewModel<ServiceCaseViewModel>.Empty;
                    IsSelected = false;
                    return;
                }

                if (Options.Length == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = IEmptyViewModel<ServiceCaseViewModel>.Empty;
                IsSelected = false;
                return;
            default: return;
        }
    }

    async Task IAsyncSearchViewModel<ServiceCaseViewModel>.Search() => await FilteredSearch();

    [RelayCommand]
    public async Task FilteredSearch()
    {
        var results = await _caseService.SearchServiceCaseHistory(Filter, OnError.DefaultBehavior(this));
        if(!results.Any())
        {
            OnZeroResults?.Invoke(this, EventArgs.Empty);
            return;
        }
        var possible = results.Select(sc => new ServiceCaseViewModel(sc));
        foreach(var vm in possible)
            vm.Selected += (_, sc) => Select(sc);

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
                    Selected = IEmptyViewModel<ServiceCaseViewModel>.Empty;
                    IsSelected = false;
                    OnZeroResults?.Invoke(this, EventArgs.Empty);
                    return;
                }

                if (Options.Length == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = IEmptyViewModel<ServiceCaseViewModel>.Empty;
                IsSelected = false;
                return;
            default: return;
        }
    }
}