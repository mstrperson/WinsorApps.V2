using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    IErrorHandling,
    IModelCarrier<MarCommEventViewModel, MarCommRequest>
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private bool printInvite;
    [ObservableProperty] private bool digitalInvite;
    [ObservableProperty] private bool newsletterReminder;
    [ObservableProperty] private bool emailReminder;
    [ObservableProperty] private bool scriptHelp;
    [ObservableProperty] private bool printedProgram;
    [ObservableProperty] private bool digitalProgram;
    [ObservableProperty] private bool needsMedia;
    [ObservableProperty] private bool needPhotographer;
    [ObservableProperty] private ObservableCollection<ContactViewModel> inviteList = [];
    [ObservableProperty] private ContactSearchViewModel contactSearch = new() { SelectionMode = SelectionMode.Single };
    [ObservableProperty] private bool hasLoaded;

    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "Working";

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

    public Optional<MarCommRequest> Model { get; private set; } = Optional<MarCommRequest>.None();

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
            [.. vm.InviteList.Select(con => con.Id)]
        );


    [RelayCommand]
    public void Clear()
    {
        Id = "";
        Model = Optional<MarCommRequest>.None();
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
        Model = Optional<MarCommRequest>.Some(model);


        InviteList = [..model.inviteList.Select(ContactViewModel.Get)];
        foreach(var contact in InviteList)
            contact.Selected += (_, _) => InviteList.Remove(contact);
        HasLoaded = true;
    }

    public static MarCommEventViewModel Empty => new();

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();
    private readonly ContactService  _contactService = ServiceHelper.GetService<ContactService>();

    [RelayCommand]
    public async Task Continue(bool template = false)
    {
        var result = await _service.PostMarComRequest(Id, this, OnError.DefaultBehavior(this));
        if (result is not null)
        {
            Model = Optional<MarCommRequest>.Some(result);
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

    private MarCommEventViewModel(MarCommRequest request)
    {
        Load(request);
    }
    public static MarCommEventViewModel Get(MarCommRequest model) => new(model);
}


