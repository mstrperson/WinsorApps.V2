using CommunityToolkit.Mvvm.ComponentModel;
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
    [ObservableProperty] VirtualEventViewModel virtualEvent = VirtualEventViewModel.Default;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";

    public static TechEventViewModel Default => new();

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    public Task Continue()
    {
        throw new NotImplementedException();
    }

    public Task Delete()
    {
        throw new NotImplementedException();
    }
}

public partial class VirtualEventViewModel :
    ObservableObject,
    IDefaultValueViewModel<VirtualEventViewModel>
{
    [ObservableProperty] bool isWebinar;
    [ObservableProperty] bool registrationRequired;
    [ObservableProperty] bool chatEnabled;
    [ObservableProperty] bool qaEnabled;
    [ObservableProperty] bool recordingEnabled;
    [ObservableProperty] bool sendReminder;
    [ObservableProperty] bool recordTranscript;
    [ObservableProperty] bool getRegistrantList;
    [ObservableProperty] bool getZoomLink;
    [ObservableProperty] string qASupportPerson = "";
    [ObservableProperty] ContactViewModel hostContact = ContactViewModel.Default;
    [ObservableProperty] bool showPanelits;
    [ObservableProperty] ContactSearchViewModel panelists = new() { SelectionMode = SelectionMode.Multiple };

    public static VirtualEventViewModel Get(VirtualEvent model)
    {
        var vm = new VirtualEventViewModel()
        {
            IsWebinar = model.webinar,
            RegistrationRequired = model.registration,
            ChatEnabled = model.chatEnabled,
            QaEnabled = model.qaEnabled,
            QASupportPerson = model.qaSupportPerson,
            RecordingEnabled = model.recording,
            SendReminder = model.reminder,
            RecordTranscript = model.transcript,
            GetRegistrantList = model.registrantList,
            GetZoomLink = model.zoomLink
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


