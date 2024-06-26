using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class EventListViewModel :
    ObservableObject,
    IErrorHandling
{
    [ObservableProperty] ImmutableArray<EventFormViewModel> events = [];

    public void AddEvents(IEnumerable<EventFormViewModel> events)
    {
        Events = Events.AddRange(events);
    }

    public static async Task<EventListViewModel> MyCreatedEvents(DateTime start, DateTime end, ErrorAction onError)
    {
        var service = ServiceHelper.GetService<EventFormsService>();
        await service.WaitForInit(onError);
        var events = await service.GetMyCreatedEvents(start, end, onError);
        return new() { Events = EventFormViewModel.GetClonedViewModels(events).ToImmutableArray() };
    }
    public static async Task<EventListViewModel> MyLeadEvents(DateTime start, DateTime end, ErrorAction onError)
    {
        var service = ServiceHelper.GetService<EventFormsService>();
        await service.WaitForInit(onError);
        var events = await service.GetMyLeadEvents(start, end, onError);
        return new() { Events = EventFormViewModel.GetClonedViewModels(events).ToImmutableArray() };
    }

    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] DateTime start;
    [ObservableProperty] DateTime end;

    Func<EventFormBase, bool> EventFilter { get; set; } = evt => true;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<EventFormViewModel>? OnEventSelected;

    [RelayCommand]
    public void CreateNew()
    {
        var vm = new EventFormViewModel();
        vm.OnError += (sender, err) => OnError?.Invoke(sender, err);
        Events = Events.Add(vm);
        OnEventSelected?.Invoke(this, vm);
    }

    [RelayCommand]
    public async Task IncrementMonth()
    {
        (Start, End) = (Start.AddMonths(1), End.AddMonths(1));
        await Reload();
    }

    [RelayCommand]
    public async Task DecrementMonth()
    {
        (Start, End) = (Start.AddMonths(-1), End.AddMonths(-1));
        await Reload();
    }

    [RelayCommand]
    public async Task IncrementWeek()
    {
        (Start, End) = (Start.AddDays(7), End.AddDays(7));
        await Reload();
    }

    [RelayCommand]
    public async Task DecrementWeek()
    {
        (Start, End) = (Start.AddDays(-7), End.AddDays(-7));
        await Reload();
    }


    [RelayCommand]
    public async Task Reload()
    {
        if (Start < _service.CacheStartDate || End > _service.CacheEndDate)
            await _service.UpdateCache(Start, End, OnError.DefaultBehavior(this));

        Events = EventFormViewModel.GetClonedViewModels(
            _service.EventsCache
            .Where(evt => EventFilter(evt) && evt.start >= Start && evt.end <= End)
            ).ToImmutableArray();

        foreach(var vm in Events)
        {
            vm.Selected += (sender, evt) => OnEventSelected?.Invoke(this, vm);
            vm.Deleted += (_, _) => Events = Events.Remove(vm);
            vm.OnError += (sender, err) => OnError?.Invoke(sender, err);
        }
    }
}
