using System.Collections.Immutable;
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
    IErrorHandling
{
    private readonly ServiceCaseService _caseService = ServiceHelper.GetService<ServiceCaseService>();
    private readonly DeviceService _deviceService = ServiceHelper.GetService<DeviceService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<ServiceCaseViewModel>? Selected;

    [RelayCommand]
    public void Select() => Selected?.Invoke(this, this);

    private ServiceCase _case;
    public ServiceCaseViewModel()
    {
        _case = new();
        loaner = new()
        {
            Available = _deviceService.Loaners
                .Select(dev => new DeviceViewModel(dev))
                .ToImmutableArray()
        };
    }

    public ServiceCaseViewModel(ServiceCase serviceCase)
    {
        _case = serviceCase;
        id = serviceCase.id;
        loaner = new()
        {
            Available = _deviceService.Loaners
                .Select(dev => new DeviceViewModel(dev))
                .ToImmutableArray()
        };
        loaner.Selected = loaner.Available.FirstOrDefault(dev => dev.WinsorDevice is not null && )
    }

    [ObservableProperty] private string id = "";

    [ObservableProperty] private DeviceSearchViewModel deviceSearch = new();

    [ObservableProperty] private UserViewModel owner =
        IEmptyViewModel<UserViewModel>.Empty;

    [ObservableProperty] private ImmutableArray<string> commonIssues = [];
    [ObservableProperty] private string intakeNotes = "";
    [ObservableProperty] private DateTime opened = DateTime.Now;
    [ObservableProperty] private bool isClosed;
    [ObservableProperty] private string status = "";
    [ObservableProperty] private ImmutableArray<DocumentViewModel> attachedDocuments = [];
    [ObservableProperty] private double repairCost = 0;
    [ObservableProperty] private DeviceSearchViewModel loaner;

}