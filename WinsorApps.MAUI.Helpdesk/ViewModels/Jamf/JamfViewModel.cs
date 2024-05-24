using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Jamf;

public partial class JamfViewModel : ObservableObject, IEmptyViewModel<JamfViewModel>, ISelectable<JamfViewModel>, IErrorHandling
{
    private readonly JamfService _jamf;

    public event EventHandler<UserViewModel>? UserSelected;
    public event EventHandler<JamfViewModel>? Selected;

    public event EventHandler<ErrorRecord>? OnError;


    public JamfViewModel() { _jamf = ServiceHelper.GetService<JamfService>(); }

    public JamfViewModel(Computer.Details computer)
    {
        _jamf = ServiceHelper.GetService<JamfService>();
        LoadComputerDetails(computer);
    }
    public JamfViewModel(MobileDevice.Details device)
    {
        _jamf = ServiceHelper.GetService<JamfService>();
        LoadDeviceDetails(device);
    }

    private void LoadComputerDetails(Computer.Details computer)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        Id = computer.id;
        Name = computer.general.name;
        AssetTag = computer.general.assetTag;
        SerialNumber = computer.hardware.serialNumber;
        Department = _jamf.Departments.FirstOrDefault(dept => dept.id == computer.userAndLocation.departmentId);
        Model = computer.hardware.model;
        if (!string.IsNullOrEmpty(computer.userAndLocation.email))
        {
            var owner = registrar.AllUsers.FirstOrDefault(u => u.email == computer.userAndLocation.email);
            User = UserViewModel.Get(owner);
            User.Selected += (sender, e) => UserSelected?.Invoke(sender, e);
        }
        Type = JamfDeviceType.Computer;
        _computer = computer;
    }
    private void LoadDeviceDetails(MobileDevice.Details device)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        Id = device.id;
        Name = device.name;
        AssetTag = device.assetTag;
        SerialNumber = device.serialNumber;
        Department = _jamf.Departments.FirstOrDefault(dept => dept.id == device.location.departmentId);
        Model = device.ios?.model ?? device.tvos?.model ?? "unknown";
        if (!string.IsNullOrEmpty(device.location.emailAddress))
        {
            var owner = registrar.AllUsers.FirstOrDefault(u => u.email == device.location.emailAddress);
            User = UserViewModel.Get(owner);
            User.Selected += (sender, e) => UserSelected?.Invoke(sender, e);
        }
        Type = JamfDeviceType.MobileDevice;
        _device = device;
    }

    private Computer.Details? _computer;
    private MobileDevice.Details? _device;

    [ObservableProperty] private string id = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string assetTag = "";
    [ObservableProperty] private string serialNumber = "";
    [ObservableProperty] private Department department;
    [ObservableProperty] private string model = "";
    [ObservableProperty] private UserViewModel user = IEmptyViewModel<UserViewModel>.Empty;
    [ObservableProperty] private JamfDeviceType type = JamfDeviceType.Computer;
    [ObservableProperty] private bool isSelected;

    [RelayCommand]
    public void Select()
    {
        Selected?.Invoke(this, this);
    }

    [RelayCommand]
    public async Task Refresh()
    {
        if(Type == "Computer")
        {
            var computer = await _jamf.GetComputerDetails(Id, OnError.DefaultBehavior(this));
            if(computer.HasValue)
                LoadComputerDetails(computer.Value);

            return;
        }

        var device = await _jamf.GetMobileDeviceDetails(Id, OnError.DefaultBehavior(this));
        if (device.HasValue)
            LoadDeviceDetails(device.Value);
    }
}

public partial class InventoryPreloadViewModel : ObservableObject, IEmptyViewModel<InventoryPreloadViewModel>, ISelectable<InventoryPreloadViewModel>, IErrorHandling
{
    private readonly JamfService _jamf;

    private InventoryPreloadEntry _entry;

    [ObservableProperty] private string id = "";
    [ObservableProperty] private string serialNumber = "";
    [ObservableProperty] private UserViewModel owner = IEmptyViewModel<UserViewModel>.Empty;
    [ObservableProperty] private string department = "";
    [ObservableProperty] private string assetTag = "";
    [ObservableProperty] private JamfDeviceType type = JamfDeviceType.Computer;
    [ObservableProperty] private bool isSelected;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<UserViewModel>? OnUserSelected;
    public event EventHandler<InventoryPreloadViewModel>? Selected;

    public InventoryPreloadViewModel() 
    {
        _jamf = ServiceHelper.GetService<JamfService>();
    }

    public InventoryPreloadViewModel(InventoryPreloadEntry entry)
    {
        _jamf = ServiceHelper.GetService<JamfService>();
        LoadEntry(entry);
    }

    private void LoadEntry(InventoryPreloadEntry entry)
    {
        _entry = entry;
        Id = entry.id;
        SerialNumber = entry.serialNumber;

        if (!string.IsNullOrEmpty(entry.emailAddress))
        {
            var registrar = ServiceHelper.GetService<RegistrarService>();
            var user = registrar.AllUsers.FirstOrDefault(u => u.email == entry.emailAddress);
            Owner = UserViewModel.Get(user);
            Owner.Selected += (sender, e) => OnUserSelected?.Invoke(sender, e);
        }

        Type = entry.deviceType;
    }

    [RelayCommand]
    public void Select() => Selected?.Invoke(this, this);

    [RelayCommand]
    public async Task Refresh()
    {
        var entry = await _jamf.GetInventoryPreload(Id, OnError.DefaultBehavior(this));
        if(entry.HasValue)
            LoadEntry(entry.Value);
    }
}
