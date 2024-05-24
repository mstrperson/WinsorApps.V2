using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Devices;

public partial class DeviceViewModel
{
    private readonly ServiceCaseService _caseService = ServiceHelper.GetService<ServiceCaseService>();

    [ObservableProperty] ImmutableArray<ServiceCaseViewModel> serviceHistory = [];
    [ObservableProperty] bool showServiceHistory;

    [RelayCommand]
    public async Task LoadServiceHistory()
    {
        var searchResults = await _caseService.SearchServiceCaseHistory(new(deviceId: Id), OnError.DefaultBehavior(this));
        ServiceHistory = searchResults.Select(sc => ServiceCaseViewModel.Get(sc)).ToImmutableArray();
        foreach(var serviceCase in ServiceHistory)
        {
            serviceCase.OnError += (sender, e) => OnError?.Invoke(sender, e);;
            serviceCase.Selected += (sender, e) => ServiceCaseSelected?.Invoke(sender, e);
        }
        ShowServiceHistory = true;
    }


    public event EventHandler<ServiceCaseViewModel>? ServiceCaseSelected;
    public event EventHandler<DeviceViewModel>? NewServiceCaseRequested;

    [RelayCommand]
    public void StartServiceCase() => NewServiceCaseRequested?.Invoke(this, this);

}
