using System.Collections.Concurrent;
using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class UserViewModel : 
    ObservableObject,
    IDefaultValueViewModel<UserViewModel>, 
    ISelectable<UserViewModel>, 
    IErrorHandling, 
    ICachedViewModel<UserViewModel, UserRecord, RegistrarService>
{

    public static async Task Initialize(RegistrarService registrar, ErrorAction onError)
    {
        while (!registrar.Ready)
            await Task.Delay(250);
        ViewModelCache = [..
            registrar.AllUsers.Select(u => new UserViewModel(u))];
        foreach (var vm in ViewModelCache)
            vm.GetUniqueDisplayName();
    }
    public static ConcurrentBag<UserViewModel> ViewModelCache { get; private set; } = [];
    //public static ConcurrentBag<UserViewModel> ViewModelCache => _viewModelCache.ToList();

    public static List<UserViewModel> GetClonedViewModels(IEnumerable<UserRecord> models)
    {
        List<UserViewModel> output = [];
        foreach (var user in models)
            output.Add(Get(user));

        return output;
    }
    public static UserViewModel Get(UserRecord user)
    {
        var vm = ViewModelCache.FirstOrDefault(cvm => cvm.User.id == user.id);
        if (vm is null)
        {
            vm = new(user);
            ViewModelCache.Add(vm);
        }
        return vm.Clone();
    }
    public UserViewModel Clone() => (UserViewModel)this.MemberwiseClone();

    private readonly RegistrarService _registrar;
    public UserRecord User;

    public string BlackbaudId => $"{User.blackbaudId}";
    public string Id => User.id;
    
    [ObservableProperty] private string displayName;
    public string Email => User.email;

    public static UserViewModel Default => new();

    [ObservableProperty] private ImmutableArray<SectionViewModel> academicSchedule = [];
    [ObservableProperty] private bool showButton = false;
    [ObservableProperty] bool isSelected;

    [ObservableProperty] private ImageSource imageSource;

    public event EventHandler<UserViewModel>? Selected;
    public event EventHandler<SectionViewModel>? SectionSelected;
    public event EventHandler<ErrorRecord>? OnError;

    public UserViewModel()
    {
        _registrar = ServiceHelper.GetService<RegistrarService>()!;
        User = new();
        displayName = "";
        ImageSource = ImageSource.FromUri(new("https://bbk12e1-cdn.myschoolcdn.com/ftpimages/1082/logo/2019-masterlogo-white.png"));
    }
    private UserViewModel(UserRecord user)
    {
        User = user;
        displayName = $"{user.firstName} {user.lastName}";
        _registrar = ServiceHelper.GetService<RegistrarService>()!;
        ImageSource = ImageSource.FromUri(new("https://bbk12e1-cdn.myschoolcdn.com/ftpimages/1082/logo/2019-masterlogo-white.png"));
    }

    [RelayCommand]
    public async Task GetPhoto()
    {

        var photo = await _registrar.GetUserPhotoAsync(Id, OnError.DefaultBehavior(this));
        if (!photo.HasValue || photo.Value.b64data.Length == 0)
        {
            ImageSource = ImageSource.FromUri(new("https://bbk12e1-cdn.myschoolcdn.com/ftpimages/1082/logo/2019-masterlogo-white.png"));
            return;
        }
        ImageSource = ImageSource.FromStream(() => new MemoryStream(photo.Value.b64data));
    }

    [RelayCommand]
    public void GetUniqueDisplayName()
    {
        try
        {
            DisplayName = _registrar.GetUniqueDisplayNameFor(User);
        }
        catch (ServiceNotReadyException)
        {
            // ignore
        }
    }


    [RelayCommand]
    public void LoadMySchedule()
    {
        var schedule = _registrar.MyAcademicSchedule;
        AcademicSchedule = schedule
            .Select(SectionViewModel.Get)
            .ToImmutableArray();
        foreach (var section in AcademicSchedule)
            section.Selected += (sender, sec) => SectionSelected?.Invoke(sender, sec); 
        ShowButton = false;
    }
    
    [RelayCommand]
    public void Select()
    {
        Selected?.Invoke(this, this);
    }
}