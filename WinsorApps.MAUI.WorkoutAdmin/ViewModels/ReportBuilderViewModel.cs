using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Athletics.Models;
using WinsorApps.Services.Athletics.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.WorkoutAdmin.ViewModels;

public partial class ReportBuilderViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly WorkoutService _service;

    public ReportBuilderViewModel(WorkoutService service, ApiService api)
    {
        _service = service;
        api.OnLoginSuccess += async (_, _) =>
            await RequestLogs();
    }

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] DateTime reportStart = DateTime.Today.MonthOf();
    [ObservableProperty] DateTime reportEnd = DateTime.Today.MonthOf().AddMonths(1);
    [ObservableProperty] WorkoutLogViewModel log = new();

    [RelayCommand]
    public async Task RequestLogs()
    {
        Busy = true;
        BusyMessage = $"Downloading Workout Logs for {ReportStart:d MMM yyyy} - {ReportEnd:d MMM yyyy}";
        var log = await _service.GetAllWorkoutLogs(
            DateOnly.FromDateTime(ReportStart), 
            DateOnly.FromDateTime(ReportEnd), 
            OnError.DefaultBehavior(this));

        Log = WorkoutLogViewModel.Get(log);
        Busy = false;
    }
}
