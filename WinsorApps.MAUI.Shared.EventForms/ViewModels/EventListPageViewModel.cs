using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.EventForms.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class EventTwoWeekListPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly ReadonlyCalendarService _calendarService;

    public EventTwoWeekListPageViewModel(ReadonlyCalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    public event EventHandler<ErrorRecord>? OnError;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";
    [ObservableProperty] private ObservableCollection<CalendarWeekViewModel> weeks = [];
    [ObservableProperty] private EventFilterViewModel filter = new();
    [ObservableProperty] private DateTime startDate = DateTime.Today.MondayOf();
    [ObservableProperty] private int numberOfWeeks = 2;

    [ObservableProperty] bool showFilter;

    [RelayCommand]
    public void ToggleShowFilter()
    {
        ShowFilter = !ShowFilter;
    }

    [RelayCommand]
    public async Task LoadWeeks()
    {
        Busy = true;
        BusyMessage = $"Loading events for {StartDate:dd MMMM} - {StartDate.AddDays((NumberOfWeeks * 7) - 1):dd MMMM}";
        var events = await _calendarService.GetEvents(new(StartDate, StartDate.AddDays((NumberOfWeeks*7) -1)), OnError.DefaultBehavior(this));
        var weeks = events.
            SeparateByKeys(evt => evt.start.MondayOf());
        Weeks = 
        [ .. 
            weeks
            .Select(week => 
                new CalendarWeekViewModel(
                    week.Key, 
                    [.. week.Value.Select(EventFormViewModel.Get)]
                ) { WeekStarts = DayOfWeek.Monday }
            )
        ];

        foreach(var week in Weeks)
        {
            week.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            week.OnError += (_, error) => OnError?.Invoke(this, error);
            week.EventSelected += (_, evt) =>
            {
                var page = new FormView(evt);
                page.TryPush();
            };
        }

        Busy = false;
    }

    [RelayCommand]
    public void ApplyFilters()
    {
        foreach(var week in Weeks)
        {
            week.ApplyFilter(Filter.Filter);
        }
    }

    [RelayCommand]
    public void ClearFilters()
    {
        Filter.ClearFilter();
        foreach (var week in Weeks)
        {
            week.ApplyFilter(Filter.Filter);
        }
    }

    [RelayCommand]
    public async Task ResetWeeks()
    {   
        StartDate = DateTime.Today.MondayOf();
        NumberOfWeeks = 2;
        await LoadWeeks();
    }

    [RelayCommand]
    public async Task StepForward()
    {
        StartDate = StartDate.AddDays(NumberOfWeeks * 7);
        await LoadWeeks();
    }

    [RelayCommand]
    public async Task StepBackward()
    {
        StartDate = StartDate.AddDays(-NumberOfWeeks * 7);
        await LoadWeeks();
    }
}
