using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.CateringManagement.ViewModels;

public partial class CateringEventsPageViewModel :
    ObservableObject,
    IBusyViewModel,
    IErrorHandling
{
    private readonly EventsAdminService _service;
    private readonly RegistrarService _registrar;
    private readonly LocalLoggingService _logging;

    private List<EventFormViewModel> _events = [];

    [ObservableProperty] ObservableCollection<EventFormViewModel> selectedEvents = [];
    [ObservableProperty] DateTime startDate;
    [ObservableProperty] DateTime endDate;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<EventFormViewModel>? EventSelected;

    private DateRange ViewDates
    {
        get => new(StartDate, EndDate);
        set
        {
            StartDate = value.start.ToDateTime(default);
            EndDate = value.end.ToDateTime(default);
        }
    }

    public CateringEventsPageViewModel(EventsAdminService service, RegistrarService registrar, LocalLoggingService logging)
    {
        _service = service;
        _registrar = registrar;
        _logging = logging;

        _service.OnCacheRefreshed += 
            (_, _) =>
            {
                Busy = true;
                BusyMessage = "Loading Event Forms";

                EventFormViewModel.ResetCache([]);
                SetLocalCache();

                SelectedEvents = [.. _events.Where(evt => ViewDates.Contains(evt.StartDate))];

                Busy = false;

            };

        ViewDates = DateRange.ThisMonth();

        this.PropertyChanged += 
            (sender, args) =>
            {
                if(args.PropertyName == nameof(StartDate))
                {
                    SelectedEvents = [.. _events
                        .Where(evt => ViewDates.Contains(evt.StartDate))
                        .OrderBy(evt => evt.StartDate)];
                }
            };
    }

    [RelayCommand]
    public void NextMonth() =>
        ViewDates = DateRange.MonthOf(StartDate.AddMonths(1).Month, StartDate.Year);

    [RelayCommand]
    public void PrevMonth() =>
        ViewDates = DateRange.MonthOf(StartDate.AddMonths(-1).Month, StartDate.Year);
    [RelayCommand]
    public void NextYear() =>
            ViewDates = DateRange.MonthOf(StartDate.Month, StartDate.Year+1);

    [RelayCommand]
    public void PrevYear() =>
        ViewDates = DateRange.MonthOf(StartDate.Month, StartDate.Year-1);


    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Loading Event Forms";

        await _service.Refresh(OnError.DefaultBehavior(this));
        EventFormViewModel.ResetCache([]);
        SetLocalCache();

        SelectedEvents = [.. _events
            .Where(evt => ViewDates.Contains(evt.StartDate))
            .OrderBy(evt => evt.StartDate)];

        Busy = false;
    }

    private void SetLocalCache()
    {
        _events = EventFormViewModel.GetClonedViewModels(
            _service.AllEvents
                .Where(evt => evt.hasCatering));
        _events.ForEach(vm =>
        {
            vm.OnError += (sender, err) => OnError?.Invoke(sender, err);
            vm.Selected += (_, _) => EventSelected?.Invoke(this, vm);
            vm.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        });
    }

    public async Task Initialize()
    {
        Busy = true;
        BusyMessage = "Loading Event Forms";

        await _service.WaitForInit(OnError.DefaultBehavior(this));
        await _registrar.WaitForInit(OnError.DefaultBehavior(this));

        SetLocalCache();

        SelectedEvents = [.. _events
            .Where(evt => ViewDates.Contains(evt.StartDate))
            .OrderBy(evt => evt.StartDate)];

        Busy = false;
    }
}
