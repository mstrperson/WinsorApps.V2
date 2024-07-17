using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;



public partial class TechEventViewModel :
    ObservableObject,
    IEventSubFormViewModel<TechEventViewModel, TechEvent>,
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
    [ObservableProperty] private bool hasLoaded;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";

    public TechEvent Model { get; private set; }

    public TechEventViewModel()
    {
        VirtualEvent.OnError += (sender, err) => OnError?.Invoke(sender, err);
        VirtualEvent.Deleted += (_, _) =>
        {
            IsVirtual = false;
            VirtualEvent.Clear();
        };
    }
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
    public void Clear()
    {
        Id = "";
        Model = default;
        VirtualEvent.Clear();
        PresenceRequested = false;
        EquipmentNeeded = false;
        HelpRequested = false;
        Details = "";
        IsVirtual = false;
    }
    public void Load(TechEvent model)
    {
        if (model == default)
        {
            Clear();
            return;
        }

        Id = model.id;
        PresenceRequested = model.presence;
        EquipmentNeeded = model.equipment;
        HelpRequested = model.help;
        Details = model.details;
        IsVirtual = model.virtualEvent.HasValue;
        Model = model;
        if (model.virtualEvent.HasValue)
        {
            VirtualEvent.Load(model.virtualEvent.Value);
        }

        HasLoaded = true;
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
    public async Task Continue(bool template = false)
    {
        var result = await _eventsService.PostTechEvent(Id, this, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            Model = result.Value;
            if(!template)
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
    IEventSubFormViewModel<VirtualEventViewModel, VirtualEvent>,
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
    [ObservableProperty] ContactSearchViewModel panelistSearch = new() { SelectionMode = SelectionMode.Single };
    [ObservableProperty] ObservableCollection<ContactViewModel> panelists = [];
    [ObservableProperty] private bool hasLoaded;

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    public VirtualEventViewModel()
    {
        PanelistSearch.OnSingleResult += (_, e) =>
        {
            if (Panelists.Any(con => con.Id == e.Id))
                return;

            var contact = ContactViewModel.Get(e.Model);
            contact.OnError += (sender, err) => OnError?.Invoke(sender, err);
            contact.Selected += (_, _) =>
            {
                Panelists.Remove(contact);
            };

            Panelists.Add(contact);
        };
    }

    public void Clear()
    {
        IsWebinar = false;
        RegistrationRequired = false;
        ChatEnabled = false;
        QaEnabled = false;
        QaSupport = false;
        QASupportPerson = "";
        RecordingEnabled = false;
        SendReminder = false;
        RecordTranscript = false;
        GetRegistrantList = false;
        GetZoomLink = false;
        ShowPanelits = false;
        HostContact = new();
        PanelistSearch.ClearSelection();
    }

    public void Load(VirtualEvent model)
    {
        if(model == default)
        {
            Clear();
            return;
        }

        IsWebinar = model.webinar;
        RegistrationRequired = model.registration;
        ChatEnabled = model.chatEnabled;
        QaEnabled = model.qaEnabled;
        QaSupport = !string.IsNullOrEmpty(model.qaSupportPerson);
        QASupportPerson = model.qaSupportPerson;
        RecordingEnabled = model.recording;
        SendReminder = model.reminder;
        RecordTranscript = model.transcript;
        GetRegistrantList = model.registrantList;
        GetZoomLink = model.zoomLink;
        ShowPanelits = model.panelists.Any();

        if(model.hostContact.HasValue)
        {
            HostContact = ContactViewModel.Get(model.hostContact.Value);
        }

        Panelists = [.. model.panelists.Select(ContactViewModel.Get)];
        foreach(var contact in  Panelists)
        {
            contact.OnError += (sender, err) => OnError?.Invoke(sender, err);
            contact.Selected += (_, _) =>
            {
                Panelists.Remove(contact);
            };
        }

        HasLoaded = true;
    }

    [RelayCommand]
    public void ToggleIsWebinar()
    {
        IsWebinar = !IsWebinar;
        WebinarLabel = IsWebinar ? "Webinar" : "Regular Zoom Meeting";
    }

    [RelayCommand]
    public async Task Continue(bool template = false)
    {
        var result = await _eventService.PostVirtualEvent(Id, this, OnError.DefaultBehavior(this));
        if(result.HasValue)
        {
            if(!template)
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
            vm.Panelists.Select(pan => pan.Id).ToImmutableArray()
        );
    public static VirtualEventViewModel Default => new();
}


