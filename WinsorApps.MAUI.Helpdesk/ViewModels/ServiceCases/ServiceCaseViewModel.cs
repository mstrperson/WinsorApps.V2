using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;

public partial class ServiceCaseViewModel : 
    ObservableObject,
    IDefaultValueViewModel<ServiceCaseViewModel>, 
    ISelectable<ServiceCaseViewModel>,
    IErrorHandling,
    IAsyncSubmit,
    ICachedViewModel<ServiceCaseViewModel, ServiceCase, ServiceCaseService>,
    IModelCarrier<ServiceCaseViewModel, ServiceCase>
{
    private readonly ServiceCaseService _caseService = ServiceHelper.GetService<ServiceCaseService>();
    private readonly DeviceService _deviceService = ServiceHelper.GetService<DeviceService>();
    private static readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<ServiceCaseViewModel>? Selected;

    public event EventHandler<ServiceCaseViewModel>? OnClose;
    public event EventHandler<ServiceCaseViewModel>? OnUpdate;
    public event EventHandler<ServiceCaseViewModel>? OnCreate;

    public ServiceCaseViewModel Self => this;

    [ObservableProperty] private string summaryText = "New Service Case";

    [RelayCommand]
    public void Select() => Selected?.Invoke(this, this);

    public Optional<ServiceCase> Model { get; private set; } = Optional<ServiceCase>.None();
    public ServiceCaseViewModel()
    {        
        StatusSearch.Select("Intake");
    }

    private ServiceCaseViewModel(ServiceCase serviceCase)
    {
        using DebugTimer _ = new($"Initializing ServiceCaseViewModel for {serviceCase.id}", _logging);
        LoadServiceCase(serviceCase);
    }

    private void LoadServiceCase(ServiceCase serviceCase)
    {
        Model = Optional<ServiceCase>.Some(serviceCase);
        Id = serviceCase.id;

        Owner = UserViewModel.Get(serviceCase.owner);
        Owner.GetPhoto().SafeFireAndForget(e => e.LogException());
        this.Device = DeviceViewModel.Get(serviceCase.device);
        Loaner =
            DeviceViewModel.Empty;
        if(!string.IsNullOrEmpty(serviceCase.loaner))
        {
            var l = _deviceService.Loaners.FirstOrDefault(dev => dev.winsorDevice is not null && dev.winsorDevice.assetTag == serviceCase.loaner);
            if(l != default)
            {
                Loaner = DeviceViewModel.Get(l);
            }
        }

        CommonIssues.Select(serviceCase.commonIssues);
        IntakeNotes = serviceCase.intakeNotes;
        Opened = serviceCase.opened;
        IsClosed = serviceCase.closed is not null;
        Closed = IsClosed ? serviceCase.closed ?? DateTime.MinValue : DateTime.MinValue;
        StatusSearch.Select(serviceCase.status);
        AttachedDocuments = [..serviceCase.attachedDocuments.Select(doc => new DocumentViewModel(doc))];
        RepairCost = serviceCase.repairCost;

        ShowNotifyButton = Status.Status.Contains("Ready");
        SummaryText = $"[{Status.Status}] {Device.DisplayName} - {Opened:dd MMM yyyy}";
    }

    [ObservableProperty] private string id = "";

    [ObservableProperty] private DeviceViewModel device = DeviceViewModel.Empty;

    [ObservableProperty] private UserViewModel owner = UserViewModel.Empty;

    [ObservableProperty] private CommonIssueSelectionViewModel commonIssues = new();
    [ObservableProperty] private string intakeNotes = "";
    [ObservableProperty] private DateTime opened = DateTime.Now;
    [ObservableProperty] private DateTime closed = DateTime.MinValue;
    [ObservableProperty] private bool isClosed;
    [ObservableProperty] private ServiceStatusSearchViewModel statusSearch = new();
    [ObservableProperty] private ObservableCollection<DocumentViewModel> attachedDocuments = [];
    [ObservableProperty] private double repairCost = 0;
    [ObservableProperty] private DeviceViewModel loaner = DeviceViewModel.Empty;
    [ObservableProperty] private bool loanerSelected;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool working;
    [ObservableProperty] private bool showNotifyButton;

    public ServiceStatusViewModel Status
    {
        get => StatusSearch.Selected;
        set => StatusSearch.Select(value);
    }
    public ObservableCollection<CommonIssueViewModel> CommonIssueList
    {
        get => CommonIssues.Selected;
        set => CommonIssues.Select([..value]);
    }

    public static List<ServiceCaseViewModel> ViewModelCache { get; private set; } = [];

    public static ServiceCaseViewModel Empty => new();

    [RelayCommand]
    public void SetLoaner(DeviceViewModel loaner)
    {
        
        Loaner = loaner;
        LoanerSelected = !string.IsNullOrEmpty(loaner.Id);
    }

    [RelayCommand]
    public async Task Submit()
    {
        Working = true;
        if (!string.IsNullOrEmpty(Id))
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Updating Service Case {Id} for {Device.DisplayName}");
            var result = await _caseService.UpdateServiceCase(new(Id, Status.Id, IntakeNotes, [..CommonIssueList.Select(issue => issue.Id)]), OnError.DefaultBehavior(this));
            Working = result is not null;
            if (result is null)
                return;

            if(result.loaner != Loaner.WinsorDevice.AssetTag)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Information,
                    $"Assigning loaner {Loaner.WinsorDevice.AssetTag} to service case {Id} for {Device.DisplayName}"); 
                var success = await _caseService.AssignLoanerToCase(Id, Loaner.WinsorDevice.AssetTag, OnError.DefaultBehavior(this));
                if (success)
                {
                    result = result with { loaner = Loaner.WinsorDevice.AssetTag };
                }
            }

            LoadServiceCase(result);
            Working = false;
            OnUpdate?.Invoke(this, this);
            return;
        }
        _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Creating new Service Case for {Device.DisplayName}");
        var newResult = await _caseService.OpenNewServiceCaseAsync(new(Device.Id, [..CommonIssueList.Select(issue => issue.Id)], IntakeNotes, Status.Id), OnError.DefaultBehavior(this));
        Working = newResult is not null;
        
        if(newResult is null) 
            return;

        if(!string.IsNullOrEmpty(Loaner.Id))
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, 
                $"Assigning loaner {Loaner.WinsorDevice.AssetTag} to service case {newResult.id} for {Device.DisplayName}");
            var success = await _caseService.AssignLoanerToCase(newResult.id, Loaner.WinsorDevice.AssetTag, OnError.DefaultBehavior(this));
            if(success)
            {
                newResult = newResult with { loaner = Loaner.WinsorDevice.AssetTag };
            }

        }

        LoadServiceCase(newResult);
        Working = false;
        OnCreate?.Invoke(this, this);
        ShowNotifyButton = Status.Status.Contains("Ready");
    }

    [RelayCommand]
    public async Task IncrementStatus()
    {
        if (Status.NextId == Status.Id)
            return;

        _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Incrementing Status for Service Case {Id} [{Device.DisplayName}]");
        Working = true;
        var success = await _caseService.IncrementCaseStatus(Id, OnError.DefaultBehavior(this));
        if (success)
        {
            Status = Status.Next;
            ShowNotifyButton = Status.Status.Contains("Ready");
            OnUpdate?.Invoke(this, this);
        }
        Working = false;
    }

    [RelayCommand]
    public async Task SendNotification()
    {
        _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Sending Pickup Notification to {Device.Owner.DisplayName} for service case {Id} {Device.DisplayName}");
        Working = true;
        await _caseService.SendPickupNotification(Id, OnError.DefaultBehavior(this));
        Working = false;
    }

    [RelayCommand]
    public async Task Close()
    {
        _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Closing service case {Id} for {Device.DisplayName}");
        Working = true;
        var result = await _caseService.CloseServiceCase(Id, OnError.DefaultBehavior(this));
        if (result)
            OnClose?.Invoke(this, this);
        Working = false;
    }

    [RelayCommand]
    public async Task PrintSticker()
    {
        _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Printing Sticker for {Id} {Device.DisplayName}");
        Working = true;
        var data = await _caseService.PrintSticker(Id, OnError.DefaultBehavior(this));
        Working = false;
        var resultTask = FileSaver.SaveAsync($"{Id}_{Device.SerialNumber}.pdf", new MemoryStream(data));
        resultTask.WhenCompleted(() =>
        {
            var result = resultTask.Result;
            if (result.IsSuccessful &&
                !string.IsNullOrEmpty(result.FilePath) &&
                Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    Process.Start(new ProcessStartInfo("msedge.exe", Uri.EscapeDataString(result.FilePath))
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    ex.LogException();
                }
            }
        });
    }

    public static List<ServiceCaseViewModel> GetClonedViewModels(IEnumerable<ServiceCase> models)
    {
        List<ServiceCaseViewModel> output = [];
        foreach (var model in models)
            output.Add(Get(model));
        return output;
    }

    public static async Task Initialize(ServiceCaseService service, ErrorAction onError)
    {
        _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Initializing ViewModelCache for ServiceCaseViewModels");
        await service.WaitForInit(onError);
        ViewModelCache = [..
            service.OpenCases.Select(sc => new ServiceCaseViewModel(sc))];
    }

    public static ServiceCaseViewModel Get(ServiceCase model)
    {
        var vm = ViewModelCache.FirstOrDefault(cvm => cvm.Id == model.id);
        if (vm is null)
        {
            vm = new ServiceCaseViewModel(model);
            ViewModelCache.Add(vm);
        }
        return vm.Clone();
    }

    public ServiceCaseViewModel Clone() => (ServiceCaseViewModel)this.MemberwiseClone();
}

public partial class ServiceCaseSearchViewModel : ObservableObject, IAsyncSearchViewModel<ServiceCaseViewModel>, IErrorHandling
{
    private readonly ServiceCaseService _caseService = ServiceHelper.GetService<ServiceCaseService>();

    [ObservableProperty] private ObservableCollection<ServiceCaseViewModel> allSelected = [];
    [ObservableProperty] private ObservableCollection<ServiceCaseViewModel> options = [];
    [ObservableProperty] private ServiceCaseViewModel selected = ServiceCaseViewModel.Empty;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool showOptions;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty] private ServiceCaseFilterViewModel filter = new();
    [ObservableProperty] private bool showFilter;

    public ServiceCaseSearchViewModel()
    {
        
    }

    public event EventHandler<ServiceCaseViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults; 
    public event EventHandler<ObservableCollection<ServiceCaseViewModel>>? OnMultipleResult;
    public event EventHandler<ErrorRecord>? OnError;

    public event EventHandler<ServiceCaseViewModel>? OnCaseClosed;
    public event EventHandler<ServiceCaseViewModel>? OnCaseUpdated;
    public event EventHandler<ServiceCaseViewModel>? OnCaseCreated;

   
    [RelayCommand]
    public void ToggleShowFilter() => ShowFilter = !ShowFilter;


    [RelayCommand]
    public void Select(ServiceCaseViewModel serviceCase)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                Selected = serviceCase;
                IsSelected = string.IsNullOrEmpty(Selected.Id);
                Options = [];
                ShowOptions = false;
                OnSingleResult?.Invoke(this, Selected);
                return;
            case SelectionMode.Multiple:
                if (AllSelected.Contains(serviceCase))
                    AllSelected = [.. AllSelected.Except([serviceCase])];
                else
                    AllSelected = [.. AllSelected, serviceCase];

                IsSelected = AllSelected.Count > 0;
                if (IsSelected)
                    OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    [RelayCommand]
    public async Task Search()
    {
        var results = await _caseService.SearchServiceCaseHistory(Filter, OnError.DefaultBehavior(this));
        if(results.Count == 0)
        {
            OnZeroResults?.Invoke(this, EventArgs.Empty);
            return;
        }
        var possible = results.Select(ServiceCaseViewModel.Get);
        foreach(var vm in possible)
            vm.Selected += (_, sc) => Select(sc);

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
                    Selected = ServiceCaseViewModel.Empty;
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
                Selected = ServiceCaseViewModel.Empty;
                IsSelected = false;
                return;
            default: return;
        }
    }
}