using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.EventForms.ViewModels;

public partial class EventsCalendarViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly ReadonlyCalendarService _calendarService;
    private readonly LocalLoggingService _logging;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] CalendarViewModel calendar;
    [ObservableProperty] EventFilterViewModel eventFilterViewModel = new();
    [ObservableProperty] bool showFilter;

    [ObservableProperty] EventFormViewModel selectedEvent = new();
    [ObservableProperty] bool showEvent;

    public event EventHandler<EventFormViewModel>? LoadEvent;

    public EventsCalendarViewModel(ReadonlyCalendarService calendarService, LocalLoggingService logging)
    {
        _calendarService = calendarService;
        _logging = logging;

        _calendarService.OnCacheRefreshed += async (_, _) => await Refresh();

        Calendar = new()
        {
            Month = DateTime.Today.MonthOf(),
            MonthlyEventSource = (date) =>
                _calendarService.EventForms
                    .Where(evt => evt.start.Month == date.Month)
                    .Select(evt => evt.details)
                    .ToImmutableArray()
        };
        Calendar.EventSelected += (_, evt) => LoadEvent?.Invoke(this, evt);
        Calendar.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
    }

    [RelayCommand]
    public void LoadSelectedEvent() => LoadEvent?.Invoke(this, SelectedEvent);

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Loading Events";
        await Calendar.LoadEvents();
        ApplyFilter();
        Busy = false;
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
        await Calendar.IncrementMonth();
    }

    [RelayCommand]
    public async Task DecrementMonth()
    {
        await Calendar.DecrementMonth();
    }

    public event EventHandler<ErrorRecord>? OnError;
}

