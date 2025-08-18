using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.BookstoreManager.ViewModels;

public partial class ReportsPageViewModel(BookstoreManagerService managerService) :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly BookstoreManagerService _managerService = managerService;

    [ObservableProperty] private bool springTerm;
    [ObservableProperty] private bool byIsbn;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public async Task GetBookOrders()
    {
        Busy = true;
        BusyMessage = "Downloading Report...";
        var result = await _managerService.DownloadReportCSV(OnError.DefaultBehavior(this), !SpringTerm, SpringTerm, ByIsbn);
        if (result.Length > 0)
        {
            using MemoryStream ms = new();
            ms.Write(result);
            ms.Position = 0;
            var fileSaveResult = await FileSaver.SaveAsync(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BookOrderReport.csv", ms);
            if (!fileSaveResult.IsSuccessful)
            {
                fileSaveResult.Exception?.LogException();
                OnError?.Invoke(this, new ErrorRecord("Report Not Saved", $"Saving File was not completed successfully. {fileSaveResult.Exception?.Message}"));
            }
        }

        Busy = false;
    }
}
