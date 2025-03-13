using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class DocumentViewModel(DocumentHeader document) : ObservableObject, IErrorHandling
{
    private readonly ApiService _api = ServiceHelper.GetService<ApiService>();
    private readonly DocumentHeader _document = document;

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] private string fileName = document.fileName;
    [ObservableProperty] private string type = document.mimeType;

    [RelayCommand]
    public async Task Download()
    {
        await using var result = 
            await _api.DownloadStream(_document.location, onError: OnError.DefaultBehavior(this));
        await FileSaver.SaveAsync(FileName, result);
    }
}