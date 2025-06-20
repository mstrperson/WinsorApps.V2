using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Clubs.Models;
using WinsorApps.Services.Clubs.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.ClubAttendance.ViewModels;

public partial class ClubViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    IModelCarrier<ClubViewModel, ClubDetails>
{
    private static readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty] bool busy;
    [ObservableProperty] string id = "";
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] string name = "";
    [ObservableProperty] string schoolYear = "";
    [ObservableProperty] UserViewModel adult = UserViewModel.Empty;
    [ObservableProperty] ObservableCollection<UserViewModel> studentLeaders = [];

    public Optional<ClubDetails> Model { get; private set; } = Optional<ClubDetails>.None();

    public event EventHandler<ErrorRecord>? OnError;

    public static ClubViewModel Get(ClubDetails model)
    {
        var vm = new ClubViewModel()
        {
            Model = Optional<ClubDetails>.Some(model),
            Id = model.id,
            Name = model.name,
            SchoolYear = model.schoolYear,
            Adult = UserViewModel.Get(model.adult),
            StudentLeaders = 
            [
               .. model.studentLeaders.Select(stu => 
                    UserViewModel.Get(_registrar.AllUsers.FirstOrNone(u => u.id == stu.id).Reduce(UserRecord.Empty)))
            ]
        };

        return vm;
    }
}

public partial class SwipeCardEntry :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    public event EventHandler<ErrorRecord>? OnError;
    private readonly ClubService _service = ServiceHelper.GetService<ClubService>();
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] string textInput = "";
    [ObservableProperty] bool clearPrevious = true;

    public byte[] Bytes
    {
        get
        {
            if (string.IsNullOrWhiteSpace(TextInput))
                return [];

            try
            {
                return Encoding.UTF8.GetBytes(TextInput);
            }
            catch (FormatException)
            {
                OnError?.Invoke(this, new ErrorRecord("Invalid Base64 string", "SwipeCardEntry.Bytes"));
                return [];
            }
        }
        set
        {
            if (value is null || value.Length == 0)
            {
                TextInput = string.Empty;
                return;
            }
            
            TextInput = Encoding.UTF8.GetString(value);
        }
    }
}

public partial class ClubPageViewModel :
    ObservableObject,
    IBusyViewModel,
    IErrorHandling
{
    private readonly ClubService _service;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Loading clubs...";
    [ObservableProperty] ClubViewModel club = new();
    public event EventHandler<ErrorRecord>? OnError;

    
    public ClubPageViewModel(ClubService service)
    {
        _service = service;
        Initialize().SafeFireAndForget(e => e.LogException());
    }

    private async Task Initialize()
    {
        Busy = true;
        BusyMessage = "Loading clubs...";

        await _service.WaitForInit(OnError.DefaultBehavior(this));

        Club = ClubViewModel.Get(_service.SelectedClub!);
        Busy = false;
    }
}
