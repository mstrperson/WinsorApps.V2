using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;
public partial class MarCommEventViewModel :
    ObservableObject,
    IDefaultValueViewModel<MarCommEventViewModel>,
    IEventSubFormViewModel<MarCommEventViewModel, MarCommRequest>,
    IBusyViewModel,
    IErrorHandling
{
    [ObservableProperty] string id = "";
    [ObservableProperty] bool printInvite;
    [ObservableProperty] bool digitalInvite;
    [ObservableProperty] bool newsletterReminder;
    [ObservableProperty] bool emailReminder;
    [ObservableProperty] bool scriptHelp;
    [ObservableProperty] bool printedProgram;
    [ObservableProperty] bool digitalProgram;
    [ObservableProperty] bool needsMedia;
    [ObservableProperty] bool needPhotographer;
    [ObservableProperty] ObservableCollection<ContactViewModel> inviteList = [];
    [ObservableProperty] ContactSearchViewModel contactSearch = new() { SelectionMode = SelectionMode.Single };

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";

    public MarCommEventViewModel()
    {
        ContactSearch.OnSingleResult += (_, con) =>
        {
            if (!InviteList.Any(c => c.Id == con.Id))
            {
                var contact = con.Clone();
                contact.Selected += (_, _) => 
                    InviteList.Remove(contact);
                InviteList.Add(contact);
            }
            ContactSearch.ClearSelection();
        };

    }

    public MarCommRequest Model { get; private set; }

    public static implicit operator NewMarCommRequest(MarCommEventViewModel vm) =>
        new(
            vm.PrintInvite,
            vm.DigitalInvite,
            vm.NewsletterReminder,
            vm.EmailReminder,
            vm.ScriptHelp,
            vm.PrintedProgram,
            vm.DigitalProgram,
            vm.NeedsMedia,
            vm.NeedPhotographer,
            vm.InviteList.Select(con => con.Id).ToImmutableArray()
        );


    [RelayCommand]
    public void Clear()
    {
        Id = "";
        Model = default;
        PrintInvite = false;
        DigitalInvite = false;
        NewsletterReminder = false;
        EmailReminder = false;
        ScriptHelp = false;
        PrintedProgram = false;
        DigitalProgram = false;
        NeedsMedia = false;
        NeedPhotographer = false;
        InviteList = [];
    }
    public void Load(MarCommRequest model)
    {
        if (model == default)
        {
            Clear();
            return;
        }

        Id = model.eventId;
        PrintInvite = model.printInvite;
        DigitalInvite = model.digitalInvite;
        NewsletterReminder = model.newsletterReminder;
        EmailReminder = model.emailReminder;
        ScriptHelp = model.scriptHelp;
        PrintedProgram = model.printedProgram;
        DigitalProgram = model.digitalProgram;
        NeedsMedia = model.needCreatedMedia;
        NeedPhotographer = model.needPhotographer;
        Model = model;


        InviteList = [..model.inviteList.Select(ContactViewModel.Get)];
        foreach(var contact in InviteList)
            contact.Selected += (_, _) => InviteList.Remove(contact);
    }

    public static MarCommEventViewModel Default => new();

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();
    private readonly ContactService  _contactService = ServiceHelper.GetService<ContactService>();

    [RelayCommand]
    public async Task Continue(bool template = false)
    {
        var result = await _service.PostMarComRequest(Id, this, OnError.DefaultBehavior(this));
        if (result.HasValue)
        {
            Model = result.Value;
            if(!template)
                ReadyToContinue?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    public async Task Delete()
    {
        var result = await _service.DeleteMarCommRequest(Id, OnError.DefaultBehavior(this));
        if (result)
            Deleted?.Invoke(this, EventArgs.Empty);
    }
}


