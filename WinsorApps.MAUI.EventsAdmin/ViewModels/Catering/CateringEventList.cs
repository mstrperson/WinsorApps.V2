using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services.Admin;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels.Catering;

public partial class CateringManagementEventListPageViewModel :
    ObservableObject,
    IBusyViewModel,
    IErrorHandling
{
    private readonly EventsAdminService _adminService;
    private readonly EventFormViewModelCacheService _cacheService;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "Loading Events...";
    [ObservableProperty] ObservableCollection<EventCateringAdminViewModel> events = [];
    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] public EventCateringAdminViewModel? selectedEvent;
    [ObservableProperty] public bool showSelectedEvent = false;

    [ObservableProperty] DateTime month = DateTime.Today.MonthOf();

    public CateringManagementEventListPageViewModel(
        EventFormViewModelCacheService cacheService, 
        EventsAdminService adminService)
    {
        _cacheService = cacheService;
        _adminService = adminService;
    }

    [RelayCommand]
    public void IncrementMonth()     
    {
        Month = Month.AddMonths(1);
        LoadEvents();
    }

    [RelayCommand]
    public void DecrementMonth()
    {
        Month = Month.AddMonths(-1);
        LoadEvents();
    }

    public async Task Initialize(ErrorAction onError)
    {
        Busy = true;
        BusyMessage = "Loading Events...";
        await _cacheService.WaitForInit(onError);
        LoadEvents();
    }

    private async Task LoadEvents()
    {
        Busy = true;
        BusyMessage = $"Loading Events for {Month:MMMM yyyy}";
        var events = await _adminService.GetAllEvents(OnError.DefaultBehavior(this), Month, Month.AddMonths(1));
        Events =
        [..
            events
                .Where(ev => ev.hasCatering)
                .Select(_cacheService.Get)
                .OrderBy(ev => ev.StartDateTime)
        ];

        foreach (var ev in Events)
        {
            await ev.Event.LoadCatering();
            ev.OnError += (sender, error) => OnError?.Invoke(sender, error);
            ev.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            ev.Event.Deleted += (_, _) =>
            {
                Events.Remove(ev);
            };
            ev.Event.Selected += (_, it) =>
            {
                SelectedEvent = it;
                ShowSelectedEvent = true;
                SelectedEvent.Closed += (_, _) =>
                {
                    ShowSelectedEvent = false;
                };
            };
        }
        Busy = false;
    }


    [RelayCommand]
    public async Task DownloadMonthlyReport()
    {
        Busy = true;
        BusyMessage = "Downloading Monthly Report";
        var data = await _adminService.DownloadMonthlyReport(Month, OnError.DefaultBehavior(this));
        if (data.Length == 0)
        {
            OnError?.Invoke(this, new ErrorRecord("No Report Data", "The report data is empty."));
            Busy = false;
            return;
        }
        
        var fileName = $"CateringReport_{Month:yyyy_MM}.csv";
        using MemoryStream ms = new(data);
        var result = await FileSaver.SaveAsync(fileName, ms);
        if(result.IsSuccessful)
        {
            // Notify user of success
            await (Application.Current?.Windows[0].Page?.DisplayAlert("Success", $"Report saved as {fileName}", "OK") ?? Task.CompletedTask);
        }
        else
        {
            // Notify user of failure
            OnError?.Invoke(this, new ErrorRecord("Save Failed", result.Exception?.Message ?? "Failed to Save Report."));
        }
        Busy = false;
    }

    [RelayCommand]
    public async Task DownloadMonthlyInvoices()
    {
        Busy = true;
        BusyMessage = "Downloading Monthly Invoices";
        var data = await _adminService.DownloadMonthlyInvoices(Month, OnError.DefaultBehavior(this));
        if (data.Length == 0)
        {
            OnError?.Invoke(this, new ErrorRecord("No Invoice Data", "The invoice data is empty."));
            Busy = false;
            return;
        }
        
        var fileName = $"CateringInvoices_{Month:yyyy_MM}.zip";
        using MemoryStream ms = new(data);
        var result = await FileSaver.SaveAsync(fileName, ms);
        if(result.IsSuccessful)
        {
            // Notify user of success
            await (Application.Current?.Windows[0].Page?.DisplayAlert("Success", $"Invoices saved as {fileName}", "OK") ?? Task.CompletedTask);
        }
        else
        {
            // Notify user of failure
            OnError?.Invoke(this, new ErrorRecord("Save Failed", result.Exception.Message ?? "Failed to save Zip File."));
        }
        Busy = false;
    }
}

public partial class EventCateringAdminViewModel : 
    ObservableObject,
    IBusyViewModel,
    IErrorHandling
{
    private readonly EventsAdminService _service = ServiceHelper.GetService<EventsAdminService>();

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "Loading Event Catering...";

    [ObservableProperty] bool editable;
    [ObservableProperty] string submitText = "Edit Form";

    
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? Closed;

    public static implicit operator EventCateringAdminViewModel(EventFormViewModel ev) =>
        new EventCateringAdminViewModel(ev);

    public EventCateringAdminViewModel(EventFormViewModel ev)
    {
        Event = ev;
        Event.OnError += (sender, error) => OnError?.Invoke(sender, error);
        Event.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
    }
    public EventFormViewModel Event { get; }

    [RelayCommand]
    public async Task ToggleEditable()
    {
        Busy = true;
        Editable = !Editable;
        SubmitText = Editable ? "Save Changes" : "Edit Form";
        if (Editable)
        {
            BusyMessage = "Opening Event for Editing...";
            await Event.StartUpdating();
        }
        else
        {
            BusyMessage = "Submitting Updates";
            await Event.Catering.Continue();
            await Event.CompleteUpdate();
        }
        Busy = false;

    }

    [RelayCommand]
    public async Task Close()
    {
        if (Editable)
            await ToggleEditable();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task UpdateLaborCost()
    {
        Busy = true;
        BusyMessage = "Updating Labor Cost";
        await _service.UpdateCateringLaborCost(Event.Id, Event.Catering.LaborCost, OnError.DefaultBehavior(this));
        Busy = false;
    }

    [RelayCommand]
    public async Task DownloadInvoice()
    {
        Busy = true;
        BusyMessage = "Downloading Invoice";
        var data = await _service.DownloadInvoice(Event.Id, OnError.DefaultBehavior(this));
        if (data.Length == 0)
        {
            OnError?.Invoke(this, new ErrorRecord("No Invoice Data", "The invoice data is empty."));
            Busy = false;
            return;
        }
        
        var fileName = $"CateringInvoice_{Event.Id}.csv";
        using MemoryStream ms = new(data);
        var result = await FileSaver.SaveAsync(fileName, ms);
        if(result.IsSuccessful)
        {
            // Notify user of success
            await (Application.Current?.Windows[0].Page?.DisplayAlert("Success", $"Invoice saved as {fileName}", "OK") ?? Task.CompletedTask);
        }
        else
        {
            // Notify user of failure
            OnError?.Invoke(this, new ErrorRecord("Save Failed", "Failed to save the invoice file."));
        }
        Busy = false;
    }
}


