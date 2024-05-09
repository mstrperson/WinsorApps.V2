using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class DocumentViewModel : ObservableObject, IErrorHandling
{
    private readonly ApiService _api = ServiceHelper.GetService<ApiService>();
    private readonly DocumentHeader _document;

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] private string fileName;
    [ObservableProperty] private string type;
    
    public DocumentViewModel(DocumentHeader document)
    {
        _document = document;
        fileName = document.fileName;
        type = document.mimeType;
    }

    [RelayCommand]
    public async Task Download()
    {
        await using var result = 
            await _api.DownloadStream(_document.location, onError: OnError.DefaultBehavior(this));
        await FileSaver.SaveAsync(FileName, result);
    }
}