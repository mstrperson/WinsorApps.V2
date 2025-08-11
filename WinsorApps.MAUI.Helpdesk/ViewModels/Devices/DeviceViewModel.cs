using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Devices;

public partial class DeviceViewModel : 
    ObservableObject,
    IDefaultValueViewModel<DeviceViewModel>,
    ISelectable<DeviceViewModel>,
    IErrorHandling,
    ICachedViewModel<DeviceViewModel, DeviceRecord, DeviceService>,
    IModelCarrier<DeviceViewModel, DeviceRecord>
{
    public static implicit operator DeviceRecord(DeviceViewModel vm) => vm.Model.Reduce(DeviceRecord.Empty);

    public override string ToString() => DisplayName;
    public static List<DeviceViewModel> ViewModelCache { get; private set; } = [];

    public static DeviceViewModel Empty => new();

    public DeviceViewModel Clone() => (DeviceViewModel)this.MemberwiseClone();

    private readonly DeviceService _deviceService;
    public Optional<DeviceRecord> Model { get; private set; }

    [ObservableProperty] private string id;
    [ObservableProperty] private string serialNumber;
    [ObservableProperty] private UserSearchViewModel ownerSearch = new();
    [ObservableProperty] private UserViewModel owner = UserViewModel.Empty;
    [ObservableProperty] private bool unicorn;
    [ObservableProperty] private DateTime firstSeen = DateTime.Today;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private string type;
    [ObservableProperty] private bool isWinsorDevice;
    [ObservableProperty] private WinsorDeviceViewModel winsorDevice = WinsorDeviceViewModel.Empty;
    [ObservableProperty] private bool isSelected;

    [ObservableProperty] private string displayName;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<DeviceViewModel>? ChangesSaved;

    public event EventHandler<DeviceViewModel>? Selected;

    public DeviceViewModel()
    {
        _deviceService = ServiceHelper.GetService<DeviceService>();
        var registrar = ServiceHelper.GetService<RegistrarService>();
        
        ownerSearch.SetAvailableUsers(registrar.AllUsers);
        ownerSearch.OnSingleResult += (_, user) => 
            Owner = user;
        Model = Optional<DeviceRecord>.None();
        displayName = "New Device";
        id = "";
        type = "";
        serialNumber = "";
        winsorDevice = WinsorDeviceViewModel.Empty;
    }

    private DeviceViewModel(DeviceRecord model)
    {
        using DebugTimer _ = new($"Initializing DeviceViewModel for {model.id}", ServiceHelper.GetService<LocalLoggingService>());
        _deviceService = ServiceHelper.GetService<DeviceService>();
        Model = Optional<DeviceRecord>.Some(model);
        displayName = model.serialNumber;
        id = model.id;
        serialNumber = model.serialNumber;
        if (model.owner is not null)
            Owner = UserViewModel.Get(model.owner);
        unicorn = model.unicorn;
        firstSeen = model.firstSeen;
        isActive = model.isActive;
        type = model.type;
        isWinsorDevice = model.isWinsorDevice;

        WinsorDevice = WinsorDeviceViewModel.Get(model);
        if (IsWinsorDevice)
            DisplayName = model.winsorDevice!.assetTag;
    }

    private CreateDeviceRecord GetCreateRecord(CreateWinsorDeviceRecord? wd = null) =>
        new(SerialNumber, Type, string.IsNullOrEmpty(Owner.Id) ? null : Owner.Id, Unicorn,
            IsActive, wd);

    private UpdateDeviceRecord GetUpdateRecord(UpdateWinsorDeviceRecord? wd = null) =>
        new(string.IsNullOrEmpty(Owner.Id) ? null : Owner.Id, Type, Unicorn, IsActive, wd);

    [RelayCommand]
    public async Task Save()
    {
        if (string.IsNullOrEmpty(Id))
        {
            var newDev = GetCreateRecord(IsWinsorDevice ? WinsorDevice.GetCreateRecord() : null);
            var result = await _deviceService
                .CreateNewDeviceRecord(newDev, OnErr);
            if (result is null)
                return;

            Model = Optional<DeviceRecord>.Some(result);
            Id = result.id;
        }
        else
        {
            var update = GetUpdateRecord(IsWinsorDevice ? WinsorDevice.GetUpdateRecord() : null);
            var updateResult = await _deviceService
                .UpdateDevice(Id, update, OnErr);

            if (updateResult is null)
                return;

            Model = Optional<DeviceRecord>.Some(updateResult);
        }

        var model = Model.Reduce(DeviceRecord.Empty);
        if (model.isWinsorDevice)
        {
            WinsorDevice = WinsorDeviceViewModel.Get(model);
            DisplayName = model.winsorDevice!.assetTag;
        }
        ChangesSaved?.Invoke(this, this);
        Select();
    }

    [RelayCommand]
    public async Task DisposeDevice()
    {
        var result = await _deviceService.DisposeDevice(Id, OnErr);
        if (result)
        {
            Model = _deviceService.DeviceCache.FirstOrNone(dev => dev.id == Id);
            IsActive = false;
            WinsorDevice = WinsorDeviceViewModel.Get(Model.Reduce(DeviceRecord.Empty));
        }
    }

    [RelayCommand]
    public void Select() => Selected?.Invoke(this, this);
    
    private void OnErr(ErrorRecord err)
    {
        OnError?.Invoke(this, err);
    }

    public static List<DeviceViewModel> GetClonedViewModels(IEnumerable<DeviceRecord> models)
    {
        List<DeviceViewModel> output = [];
        foreach(var model in models)
        {
            var vm = ViewModelCache.FirstOrDefault(cvm => cvm.Id == model.id);
            if(vm is null)
            {
                vm = new(model);
                ViewModelCache.Add(vm);
            }

            output.Add(vm.Clone());
        }

        return output;
    }

    public static async Task Initialize(DeviceService service, ErrorAction onError)
    {
        while (!service.Ready)
            await Task.Delay(250);
        ViewModelCache = [];
        //..
        //    service.DeviceCache.Select(dev => new DeviceViewModel(dev))];
    }

    public static DeviceViewModel Get(DeviceRecord model) => GetClonedViewModels([model])[0];
}