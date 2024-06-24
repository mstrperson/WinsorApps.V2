using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
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



public partial class TechEventViewModel :
    ObservableObject,
    IEventSubFormViewModel,
    IDefaultValueViewModel<TechEventViewModel>,
    IBusyViewModel,
    IErrorHandling
{
    private readonly EventFormsService _eventsService = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] bool presenceRequested;
    [ObservableProperty] bool equipmentNeeded;
    [ObservableProperty] bool helpRequested;
    [ObservableProperty] string details = "";
    [ObservableProperty] bool isVirtual;
    [ObservableProperty] VirtualEventViewModel virtualEvent = VirtualEventViewModel.Default;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";

    public static TechEventViewModel Create(string eventId) =>  new TechEventViewModel() { Id = eventId };

    [RelayCommand]
    public void AddVirtualDetails()
    {
        IsVirtual = true;
        VirtualEvent = new() { Id = this.Id };
        VirtualEvent.OnError += (sender, err) => OnError?.Invoke(sender, err);
        VirtualEvent.Deleted += (_, _) =>
        {
            IsVirtual = false;
            VirtualEvent = VirtualEventViewModel.Default;
        };
    }

    public static TechEventViewModel Get(TechEvent model)
    {
        var vm = new TechEventViewModel()
        {
            Id = model.id,
            PresenceRequested = model.presence,
            EquipmentNeeded = model.equipment,
            HelpRequested = model.help,
            Details = model.details,
            IsVirtual = model.virtualEvent.HasValue
        };
        if (model.virtualEvent.HasValue)
        {
            vm.VirtualEvent = VirtualEventViewModel.Get(model.virtualEvent.Value);
            vm.VirtualEvent.OnError += (sender, err) => vm.OnError?.Invoke(sender, err);
            vm.VirtualEvent.Deleted += (_, _) =>
            {
                vm.IsVirtual = false;
                vm.VirtualEvent = VirtualEventViewModel.Default;
            };
        }
        return vm;
    }

    public static implicit operator NewTechEvent(TechEventViewModel vm) =>
        new(
            vm.PresenceRequested,
            vm.EquipmentNeeded,
            vm.HelpRequested,
            vm.Details,
            vm.IsVirtual ? (NewVirtualEvent)vm.VirtualEvent : null
        );

    public static TechEventViewModel Default => new();

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public async Task Continue()
    {
        var result = await _eventsService.PostTechEvent(Id, this, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            ReadyToContinue?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    public async Task Delete()
    {
        var success = await _eventsService.DeleteTechEvent(Id, OnError.DefaultBehavior(this));
        if(success)
        {
            Deleted?.Invoke(this, EventArgs.Empty);
        }
    }
}

public partial class VirtualEventViewModel :
    ObservableObject,
    IDefaultValueViewModel<VirtualEventViewModel>,
    IEventSubFormViewModel,
    IErrorHandling
{
    private readonly EventFormsService _eventService = ServiceHelper.GetService<EventFormsService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] bool isWebinar;
    [ObservableProperty] string webinarLabel = "Regular Zoom Meeting";
    [ObservableProperty] bool registrationRequired;
    [ObservableProperty] bool chatEnabled;
    [ObservableProperty] bool qaEnabled;
    [ObservableProperty] bool recordingEnabled;
    [ObservableProperty] bool sendReminder;
    [ObservableProperty] bool recordTranscript;
    [ObservableProperty] bool getRegistrantList;
    [ObservableProperty] bool getZoomLink;
    [ObservableProperty] bool qaSupport;
    [ObservableProperty] string qASupportPerson = "";
    [ObservableProperty] ContactViewModel hostContact = ContactViewModel.Default;
    [ObservableProperty] bool showPanelits;
    [ObservableProperty] ContactSearchViewModel panelists = new() { SelectionMode = SelectionMode.Multiple };

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    public static VirtualEventViewModel Get(VirtualEvent model)
    {
        var vm = new VirtualEventViewModel()
        {
            IsWebinar = model.webinar,
            RegistrationRequired = model.registration,
            ChatEnabled = model.chatEnabled,
            QaEnabled = model.qaEnabled,
            QaSupport = !string.IsNullOrEmpty(model.qaSupportPerson),
            QASupportPerson = model.qaSupportPerson,
            RecordingEnabled = model.recording,
            SendReminder = model.reminder,
            RecordTranscript = model.transcript,
            GetRegistrantList = model.registrantList,
            GetZoomLink = model.zoomLink,
            ShowPanelits = model.panelists.Any()
        };

        if(model.hostContact.HasValue)
        {
            vm.HostContact = ContactViewModel.Get(model.hostContact.Value);
        }
        foreach (var panelist in vm.Panelists.Available)
        {
            panelist.IsSelected = model.panelists.Any(contact => contact.id == panelist.Id);
            vm.Panelists.AllSelected = vm.Panelists.Available.Where(con => con.IsSelected).ToImmutableArray();
        }

        return vm;
    }

    [RelayCommand]
    public void ToggleIsWebinar()
    {
        IsWebinar = !IsWebinar;
        WebinarLabel = IsWebinar ? "Webinar" : "Regular Zoom Meeting";
    }

    [RelayCommand]
    public async Task Continue()
    {
        var result = await _eventService.PostVirtualEvent(Id, this, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            ReadyToContinue?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    public async Task Delete()
    {
        var success = await _eventService.DeleteVirtualEvent(Id, OnError.DefaultBehavior(this));
        if (success)
            Deleted?.Invoke(this, EventArgs.Empty);
    }

    public static implicit operator NewVirtualEvent(VirtualEventViewModel vm) =>
        new(
            vm.IsWebinar,
            vm.RegistrationRequired,
            vm.ChatEnabled,
            vm.QaEnabled,
            vm.QASupportPerson,
            vm.RecordingEnabled,
            vm.SendReminder,
            vm.RecordTranscript,
            vm.GetRegistrantList,
            vm.GetZoomLink,
            vm.HostContact.Id,
            vm.Panelists.AllSelected.Select(pan => pan.Id).ToImmutableArray()
        );
    public static VirtualEventViewModel Default => new();
}


