using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;
public partial class MarCommEventViewModel :
    ObservableObject,
    IDefaultValueViewModel<MarCommEventViewModel>,
    IEventSubFormViewModel,
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
    [ObservableProperty] ContactSearchViewModel inviteList = new ContactSearchViewModel() { SelectionMode = SelectionMode.Multiple };

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Working";

    public MarCommRequest Request { get; private set; }

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
            vm.InviteList.AllSelected.Select(con => con.Id).ToImmutableArray()
        );
    public static MarCommEventViewModel Get(MarCommRequest model)
    {
        if (model == default)
            return new();

        var vm = new MarCommEventViewModel()
        {
            Id = model.eventId,
            PrintInvite = model.printInvite,
            DigitalInvite = model.digitalInvite,
            NewsletterReminder = model.newsletterReminder,
            EmailReminder = model.emailReminder,
            ScriptHelp = model.scriptHelp,
            PrintedProgram = model.printedProgram,
            DigitalProgram = model.digitalProgram,
            NeedsMedia = model.needCreatedMedia,
            NeedPhotographer = model.needPhotographer,
            Request = model
        };

        foreach(var contact in vm.InviteList.Available)
        {
            contact.IsSelected = model.inviteList.Any(con => con.id == contact.Id);
        }

        vm.InviteList.AllSelected = vm.InviteList.Available.Where(con => con.IsSelected).ToImmutableArray();

        return vm;
    }

    public static MarCommEventViewModel Default => new();

    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;
    public event EventHandler<ErrorRecord>? OnError;

    private readonly EventFormsService _service = ServiceHelper.GetService<EventFormsService>();

    [RelayCommand]
    public async Task Continue()
    {
        var result = await _service.PostMarComRequest(Id, this, OnError.DefaultBehavior(this));
        if (result.HasValue)
        {
            Request = result.Value;
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


