using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.EventForms.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class EventListViewModel :
    ObservableObject,
    IErrorHandling
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

    [ObservableProperty] string pageLabel = "My Events List";

    Func<EventFormBase, bool> EventFilter { get; set; } = evt => true;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<EventFormViewModel>? OnEventSelected;

    [RelayCommand]
    public void CreateNew()
    {
        var vm = new EventFormViewModel(); 
        
        vm.OnError += (sender, err) =>
            OnError?.Invoke(sender, err);
        vm.MarCommRequested += (sender, mvm) => PageRequested?.Invoke(this, new MarComPage() { BindingContext = mvm });
        vm.TheaterRequested += (sender, thvm) => PageRequested?.Invoke(this, new TheaterPage() { BindingContext = thvm });
        vm.TechRequested += (sender, tvm) => PageRequested?.Invoke(this, new TechPage(tvm));
        vm.CateringRequested += (sender, cvm) => PageRequested?.Invoke(this, new CateringPage(cvm));
        vm.FieldTripRequested += (sender, ftvm) => PageRequested?.Invoke(this, new FieldTripPage() { BindingContext = ftvm });
        vm.FacilitesRequested += (sender, fvm) => PageRequested?.Invoke(this, new FacilitesPage() { BindingContext = fvm });

        vm.TemplateRequested += (sender, vm) =>
        {
            PopThenPushRequested?.Invoke(this, new FormEditor(vm));
        };
        vm.OnError += (sender, err) => OnError?.Invoke(sender, err);
        Events.Add(vm);
        OnEventSelected?.Invoke(this, vm);
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
        if (Start < _service.CacheStartDate || End > _service.CacheEndDate)
            await _service.UpdateCache(Start, End, OnError.DefaultBehavior(this));

        Events = [.. EventFormViewModel.GetClonedViewModels(
            _service.EventsCache
            .Where(evt => EventFilter(evt) && evt.start >= Start && evt.end <= End))];

        foreach(var vm in Events)
        {
            vm.Selected += (sender, evt) => OnEventSelected?.Invoke(this, vm);
            vm.Deleted += (_, _) => Events.Remove(vm);

            vm.OnError += (sender, err) =>
                OnError?.Invoke(sender, err);
            vm.MarCommRequested += (sender, mvm) => PageRequested?.Invoke(this, new MarComPage() { BindingContext = mvm });
            vm.TheaterRequested += (sender, thvm) => PageRequested?.Invoke(this, new TheaterPage() { BindingContext = thvm });
            vm.TechRequested += (sender, tvm) => PageRequested?.Invoke(this, new TechPage(tvm));
            vm.CateringRequested += (sender, cvm) => PageRequested?.Invoke(this, new CateringPage(cvm));
            vm.FieldTripRequested += (sender, ftvm) => PageRequested?.Invoke(this, new FieldTripPage() { BindingContext = ftvm });
            vm.FacilitesRequested += (sender, fvm) => PageRequested?.Invoke(this, new FacilitesPage() { BindingContext = fvm });

            vm.TemplateRequested += (sender, vm) =>
            {
                PopThenPushRequested?.Invoke(this, new FormEditor(vm));
            };
        }
    }
}
