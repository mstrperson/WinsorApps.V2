using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class EventListViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    [ObservableProperty] ObservableCollection<EventFormViewModel> events = [];

    public event EventHandler<ContentPage>? PopThenPushRequested;
    public event EventHandler<ContentPage>? PageRequested;

    public void AddEvents(IEnumerable<EventFormViewModel> events)
    {
        Events = [ .. Events, .. events];
    }

    public static async Task<EventListViewModel> MyCreatedEvents(DateTime start, DateTime end, ErrorAction onError)
    {
        var service = ServiceHelper.GetService<EventFormsService>();
        await service.WaitForInit(onError);
        var events = await service.GetMyCreatedEvents(start, end, onError);
        var lvm = new EventListViewModel() { Events = [..EventFormViewModel.GetClonedViewModels(events)] };
        foreach(var vm in lvm.Events)
        {
            vm.Selected += (sender, evt) => lvm.OnEventSelected?.Invoke(lvm, vm);
            vm.Deleted += (_, _) => lvm.Events.Remove(vm);
            vm.OnError += (sender, err) => lvm.OnError?.Invoke(sender, err);
        }
        return lvm;
    }
    public static async Task<EventListViewModel> MyLeadEvents(DateTime start, DateTime end, ErrorAction onError)
    {
        var service = ServiceHelper.GetService<EventFormsService>();
        await service.WaitForInit(onError);
        var events = await service.GetMyLeadEvents(start, end, onError);
        var lvm = new EventListViewModel() { Events = [.. EventFormViewModel.GetClonedViewModels(events)] };
        foreach (var vm in lvm.Events)
        {
            vm.Selected += (sender, evt) => lvm.OnEventSelected?.Invoke(lvm, vm);
            vm.Deleted += (_, _) => lvm.Events.Remove(vm);
            vm.OnError += (sender, err) => lvm.OnError?.Invoke(sender, err);
        }
        return lvm;
    }

    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] DateTime start;
    [ObservableProperty] DateTime end;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "Working...";
    [ObservableProperty] string pageLabel = "My Events List";

    Func<EventFormBase, bool> EventFilter { get; set; } = evt => true;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<EventFormViewModel>? OnEventSelected;

    [RelayCommand]
    public void CreateNew()
    {
        Busy = true;
        var vm = new EventFormViewModel(); 
        
        vm.StatusSelection.Select("Draft");
        vm.IsNew = true;
        vm.IsUpdating = false;
        vm.IsSelected = false;
        vm.IsCreating = false;
        vm.CanEditBase = true;
        vm.CanEditSubForms = false;

        vm.Selected += (_, e) => OnEventSelected?.Invoke(this, e);
        
        Events.Add(vm);
        OnEventSelected?.Invoke(this, vm);
        Busy = false;
    }

    [RelayCommand]
    public async Task IncrementMonth()
    {
        (Start, End) = (Start.AddMonths(1), End.AddMonths(1));

        PageLabel = $"{Start:MMMM yyyy}";

        await Reload();
    }

    [RelayCommand]
    public async Task DecrementMonth()
    {
        (Start, End) = (Start.AddMonths(-1), End.AddMonths(-1));
        PageLabel = $"{Start:MMMM yyyy}";
        await Reload();
    }

    [RelayCommand]
    public async Task IncrementWeek()
    {
        (Start, End) = (Start.AddDays(7), End.AddDays(7));
        PageLabel = $"{Start:dd MMMM yyyy}";
        await Reload();
    }

    [RelayCommand]
    public async Task DecrementWeek()
    {
        (Start, End) = (Start.AddDays(-7), End.AddDays(-7));
        PageLabel = $"{Start:dd MMMM yyyy}";
        await Reload();
    }


    [RelayCommand]
    public async Task Reload()
    {
        Busy = true;
        BusyMessage = $"Loading Events for {Start:MMMM yyyy}";
        
        if (Start < _service.CacheStartDate || End > _service.CacheEndDate)
            await _service.UpdateCache(Start, End, OnError.DefaultBehavior(this));

        Events = [.. EventFormViewModel.GetClonedViewModels(
            _service.EventsCache
            .Where(evt => EventFilter(evt) && evt.start >= Start && evt.end <= End))];

        foreach(var vm in Events)
        {
            vm.Selected += (sender, evt) => OnEventSelected?.Invoke(this, vm);
            vm.Deleted += (_, _) => Events.Remove(vm);
        }

        Busy = false;
    }
}
