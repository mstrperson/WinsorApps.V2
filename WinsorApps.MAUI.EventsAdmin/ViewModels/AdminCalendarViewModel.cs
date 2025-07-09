using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services.Admin;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels;

public partial class AdminCalendarViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly EventsAdminService _admin = ServiceHelper.GetService<EventsAdminService>();
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] private CalendarViewModel calendar;
    [ObservableProperty] private EventFilterViewModel eventFilterViewModel = new();
    [ObservableProperty] private bool showFilter = true;
    
    [ObservableProperty] private EventFormViewModel selectedEvent = new();
    [ObservableProperty] private bool showEvent;

    [ObservableProperty] private bool busy = true;
    [ObservableProperty] private string busyMessage = "Initializing...";

    [ObservableProperty] private bool hasLoaded;
    public event EventHandler<ErrorRecord>? OnError;

    public AdminCalendarViewModel()
    {
        calendar = new()
        {
            Month = DateTime.Today.MonthOf(),
            MonthlyEventSource = (month) =>
            {
                using DebugTimer _ = new($"Filtering Events from {month:MMMM yyyy}", _logging);
                return _admin.AllEvents.Where(evt =>
                        evt.start.Month == month.Month
                        && evt.status != ApprovalStatusLabel.Creating
                        && evt.status != ApprovalStatusLabel.Updating
                        && evt.status != ApprovalStatusLabel.Withdrawn
                        && evt.status != ApprovalStatusLabel.Declined)
                    .ToList();
            }
        };
        Calendar.EventSelected += (_, evt) =>
        {
            SelectedEvent = evt;
            ShowEvent = true;
        };
        _admin.OnCacheRefreshed += async (_, _) =>
        {
            Busy = true;
            BusyMessage = "Refreshing Event List";
            await Calendar.LoadEvents();
            ApplyFilter();
            Busy = false;
        };
    }

    [RelayCommand]
    public async Task DownloadAllVisible()
    {
        Busy = true;
        BusyMessage = "Downloading all visible events...";
        var events =
            Calendar.Weeks
                .SelectMany(week => week.Days
                    .SelectMany(day => day.FilteredEvents))
                .ToList();

        var invalidChars = Path.GetInvalidFileNameChars();
        
        var evtCount = events.Count;
        BusyMessage = $"Downloading {evtCount} events...";
        List<(string, byte[])> downloads = [];
        foreach (var evt in events)
        {
            var pdf = await evt.GetPdfData();
            var summary = evt.Summary;
            foreach (var c in summary.ToArray())
            {
                if (invalidChars.Contains(c))
                    summary = summary.Replace($"{c}", "");
            }

            var startTime = evt.Model.Reduce(EventFormBase.Empty).start;
            
            downloads.Add(($"({startTime:MM-dd hh-mm tt}) {summary}.pdf", pdf));
            
            BusyMessage = $"Downloading {--evtCount} events...";
        }
        
        BusyMessage = "Zipping downloaded events...";

        using MemoryStream ms = new();
        using (ZipArchive archive = new(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (fileName, bytes) in downloads)
            {
                var entry = archive.CreateEntry(fileName);
                await using var stream = entry.Open();
                stream.Write(bytes);
                await stream.FlushAsync();
            }

            await ms.FlushAsync();
        }

        ms.Position = 0;
        BusyMessage = "Saving Downloaded events...";
        var result = await FileSaver.SaveAsync($"{Calendar.Month:MMMM} Events.zip", ms);
        if(!result.IsSuccessful)
            OnError?.Invoke(this, new("File Not Saved", result.Exception?.Message ?? ""));

        if (result is { IsSuccessful: true, FilePath: not null })
        {
            ProcessStartInfo psi = new($"{result.FilePath}")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(psi);
        }
        
        Busy = false;
    }
    
    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Loading Events";
        await Calendar.LoadEvents();
        ApplyFilter();
        Busy = false;
        HasLoaded = true;
    }

    [RelayCommand]
    public void CloseSelectedEvent()
    {
        ShowEvent = false;
    }

    [RelayCommand]
    public async Task ToggleShowFilter()
    {
        ShowFilter = !ShowFilter;
        if (!ShowFilter)
        {
            Busy = true;
            BusyMessage = "Clearing Filter";
            EventFilterViewModel.ClearFilter();
            await Calendar.LoadEvents();
            Busy = false;
        }
    }

    [RelayCommand]
    public void ApplyFilter()
    {
        Calendar.ApplyFilter(EventFilterViewModel.Filter);
    }

    [RelayCommand]
    public async Task IncrementMonth()
    {
        Busy = true;
        BusyMessage = "Loading Next Month";
        await Calendar.IncrementMonth();
        if (Calendar.Month < _admin.CacheStartDate || Calendar.Month.AddMonths(1).AddDays(-1) > _admin.CacheEndDate)
        {
            BusyMessage = $"Downloading Event Data for {Calendar.Month:MMMM yyyy}";
            _ = await _admin.GetAllEvents(OnError.DefaultBehavior(this), Calendar.Month, Calendar.Month.AddMonths(1));
        }
        Busy = false;
    }


    [RelayCommand]
    public async Task DecrementMonth()
    {
        Busy = true;
        BusyMessage = "Loading Previous Month";
        await Calendar.DecrementMonth();
        if (Calendar.Month < _admin.CacheStartDate || Calendar.Month.AddMonths(1).AddDays(-1) > _admin.CacheEndDate)
        {
            BusyMessage = $"Downloading Event Data for {Calendar.Month:MMMM yyyy}";
            _ = await _admin.GetAllEvents(OnError.DefaultBehavior(this), Calendar.Month, Calendar.Month.AddMonths(1)); 
        }
        Busy = false;
    }
}
