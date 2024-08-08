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
    ICachedViewModel<UserViewModel, UserRecord, RegistrarService>,
    IModelCarrier<UserViewModel, UserRecord>
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

    public static List<UserViewModel> GetClonedViewModels(IEnumerable<UserRecord> models)
    {
        List<UserViewModel> output = [];
        foreach (var user in models)
            output.Add(Get(user));

        return output;
    }
    public static UserViewModel Get(UserRecord user)
    {
        var vm = ViewModelCache.FirstOrDefault(cvm => cvm.Model.Reduce(UserRecord.Empty).id == user.id);
        if (vm is null)
        {
            vm = new(user);
            ViewModelCache.Add(vm);
        }
        return vm.Clone();
    }
    public UserViewModel Clone() => new()
    {
        ImageSource = ImageSource,
        IsSelected = false,
        DisplayName = DisplayName,
        Model = Model with { },
        AcademicSchedule = [.. AcademicSchedule.Select(sec => sec.Clone())],
        ShowButton = ShowButton
    };

    private readonly RegistrarService _registrar;
    public OptionalStruct<UserRecord> Model { get; private set; } = OptionalStruct<UserRecord>.None();

    public string BlackbaudId => $"{Model.Reduce(UserRecord.Empty).blackbaudId}";
    public string Id => Model.Reduce(UserRecord.Empty).id ?? "";
    
    [ObservableProperty] private string displayName;
    public string Email => Model.Reduce(UserRecord.Empty).email;

    public static UserViewModel Empty => new();

    [ObservableProperty] private ImmutableArray<SectionViewModel> academicSchedule = [];
    [ObservableProperty] private bool showButton = false;
    [ObservableProperty] bool isSelected = false;

    [ObservableProperty] private ImageSource imageSource;

    public event EventHandler<UserViewModel>? Selected;
    public event EventHandler<SectionViewModel>? SectionSelected;
    public event EventHandler<ErrorRecord>? OnError;

    public UserViewModel()
    {
        _registrar = ServiceHelper.GetService<RegistrarService>()!;
        Model = new();
        displayName = "";
        ImageSource = ImageSource.FromUri(new("https://bbk12e1-cdn.myschoolcdn.com/ftpimages/1082/logo/2019-masterlogo-white.png"));
    }
    private UserViewModel(UserRecord user)
    {
        Model = OptionalStruct<UserRecord>.Some(user);
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
            DisplayName = _registrar.GetUniqueDisplayNameFor(Model.Reduce(UserRecord.Empty));
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
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}