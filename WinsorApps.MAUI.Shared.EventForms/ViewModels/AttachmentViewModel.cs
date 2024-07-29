using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class AttachmentViewModel :
    ObservableObject,
    IModelCarrier<AttachmentViewModel, DocumentHeader>,
    IErrorHandling,
    IBusyViewModel
{
    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string fileName = "";
    [ObservableProperty] string mimeType = "";
    [ObservableProperty] string location = "";
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Downloading...";

    public DocumentHeader Model { get; private set; }

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<AttachmentViewModel>? DeleteRequested;

    public static AttachmentViewModel Get(DocumentHeader model) => new()
    {
        Id = model.id,
        FileName = model.fileName,
        MimeType = model.mimeType,
        Location = model.location,
        Model = model
    };

    [RelayCommand]
    public void Delete()
    {
        DeleteRequested?.Invoke(this, this);
    }

    [RelayCommand]
    public async Task Download()
    {
        Busy = true;
        var data = await _service.DownloadAttachment(Id, OnError.DefaultBehavior(this));
        if (data.Length == 0)
        {
            Busy = false;
            return;
        }
        using MemoryStream ms = new(data);

        var result = await FileSaver.Default.SaveAsync(_logging.DownloadsDirectory, FileName, ms);
        if(!result.IsSuccessful)
        {
            result.Exception?.LogException();
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Downloaded Attachment {FileName} was not saved.");
        }
        Busy = false;
    }
}

public partial class AttachmentCollectionViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] string eventId = "";
    [ObservableProperty] ObservableCollection<AttachmentViewModel> attachments = [];
    [ObservableProperty] bool isTheater;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working...";

    public AttachmentCollectionViewModel() { }

    public AttachmentCollectionViewModel(EventFormBase evt)
    {
        this.eventId = evt.id;
        attachments = [.. evt.attachments?.Select(AttachmentViewModel.Get) ?? []];
        foreach (var attachment in Attachments)
        {
            attachment.OnError += (sender, e) => OnError?.Invoke(sender, e);
            attachment.DeleteRequested += Attachment_DeleteRequested;
        }
    }

    public AttachmentCollectionViewModel(TheaterEvent evt)
    {
        this.eventId = evt.eventId;
        attachments = [.. evt.attachments.Select(AttachmentViewModel.Get)];
        IsTheater = true;
        foreach (var attachment in Attachments)
        {
            attachment.OnError += (sender, e) => OnError?.Invoke(sender, e);
            attachment.DeleteRequested += Attachment_DeleteRequested;
        }
    }

    private async void Attachment_DeleteRequested(object? sender, AttachmentViewModel e)
    {
        if(await _service.DeleteAttachment(EventId, e.Id, OnError.DefaultBehavior(this)))
        {
            Attachments.Remove(e);
        }
    }

    [RelayCommand]
    public async Task UploadAttachment()
    {
        var file = await FilePicker.Default.PickAsync();
        if(file is null)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Attachment Upload was Canceled.");
            return;
        }

        Busy = true;
        BusyMessage = $"Uploading attachment: {file.FileName}.";

        var header = new DocumentHeader("", file.FileName, file.ContentType, "");
        using var stream = await file.OpenReadAsync();
        var data = new byte[stream.Length];
        stream.Read(data);
        var result = IsTheater ?
            await _service.PostTheaterAttachment(EventId, header, data, OnError.DefaultBehavior(this)) :
            await _service.UploadAttachment(EventId, header, data, OnError.DefaultBehavior(this));

        if(result.HasValue)
        {
            var newAttachment = AttachmentViewModel.Get(result.Value);
            newAttachment.OnError += (sender, e) => OnError?.Invoke(sender, e);
            newAttachment.DeleteRequested += Attachment_DeleteRequested;
            Attachments.Add(newAttachment);
        }

        Busy = false;
    }

    [RelayCommand]
    public async Task DownloadAll()
    {
        if (!Attachments.Any()) return;
        Busy = true;
        BusyMessage = "Downloading all attachments.";

        Dictionary<AttachmentViewModel, byte[]> allFiles = [];

        foreach(var attachment in Attachments)
        {
            BusyMessage = $"Downloading {attachment.FileName}";
            var data = await _service.DownloadAttachment(attachment.Id, OnError.DefaultBehavior(this));
            if (data.Length == 0)
                continue;

            allFiles.Add(attachment, data);
        }

        if(allFiles.Count == 0)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"No Attachments were successfully downloaded...");
            Busy = false;
            return;
        }

        BusyMessage = "Zipping up the downloads.";

        using MemoryStream ms = new();
        using ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, true);

        foreach(var attachment in allFiles.Keys)
        {
            var entry = zip.CreateEntry(attachment.FileName);
            using var entryStream = entry.Open();
            var bytes = allFiles[attachment];
            entryStream.Write(bytes, 0, bytes.Length);
            entryStream.Flush();
        }

        ms.Seek(0, SeekOrigin.Begin);

        BusyMessage = "Saving Download.";

        var result = await FileSaver.Default.SaveAsync(_logging.DownloadsDirectory, "attachments.zip", ms);

        if(!result.IsSuccessful)
        {
            result.Exception?.LogException();
            _logging.LogMessage(LocalLoggingService.LogLevel.Information,
                $"Saving zip file containing {allFiles.Count} files was not completed successfully.");
        }

        Busy = false;
    }
}
