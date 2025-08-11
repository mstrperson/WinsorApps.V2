using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.EventsAdmin.ViewModels;
using WinsorApps.MAUI.Shared;
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
    private readonly EventFormViewModelCacheService _eventFormViewModelCacheService;
    private readonly ReadonlyCalendarService _calendarService;
    private readonly LocalLoggingService _logging;

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";
    [ObservableProperty] private CalendarViewModel calendar;
    [ObservableProperty] private EventFilterViewModel eventFilterViewModel = new();
    [ObservableProperty] private bool showFilter;

    [ObservableProperty] private EventFormViewModel selectedEvent = new();
    [ObservableProperty] private bool showEvent;

    public event EventHandler<EventFormViewModel>? LoadEvent;

    public EventsCalendarViewModel(ReadonlyCalendarService calendarService, LocalLoggingService logging, EventFormViewModelCacheService eventFormViewModelCacheService)
    {
        _calendarService = calendarService;
        _logging = logging;

        _calendarService.OnCacheRefreshed += async (_, _) => await Refresh();

        _eventFormViewModelCacheService = eventFormViewModelCacheService;
        Calendar = new()
        {
            ViewModelFactory = (evtlist) =>
            {
                Busy = true;
                BusyMessage = "Populating Calendar";
                var evts = evtlist.Select(evt =>
                    {
                        var vm = _eventFormViewModelCacheService.Get(evt);
                        vm.OnError += (sender, err) => OnError?.Invoke(sender, err);
                        vm.Selected += (sender, evt) => LoadEvent?.Invoke(this, evt);
                        vm.Deleted += async (_, _) => await Calendar.LoadEvents();
                        return vm;
                    })
                    .ToList();
                Busy = false;
                return [.. evts];
            },
            Month = DateTime.Today.MonthOf(),
            MonthlyEventSource = (date) =>
            {
                Busy = true;
                BusyMessage = $"Loading Events for {date:MMMM yyyy}";
                if (date < _calendarService.EventForms.Select(evt => evt.start).OrderBy(dt => dt).FirstOrDefault())
                    _calendarService.ManualLoadData(OnError.DefaultBehavior(this), date.Month switch
                    {
                        < 6 => date.Year - 1,
                        _ => date.Year
                    }).Wait();
                Busy = false;
                return [.._calendarService.EventForms
                    .Where(evt => evt.start.Month == date.Month)
                    .Select(evt => evt.details)];
            }
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

