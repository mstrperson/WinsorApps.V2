using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Helpdesk.ViewModels.Jamf;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels;

public partial class WinsorDeviceViewModel : ObservableObject, IErrorHandling, ICachedViewModel<WinsorDeviceViewModel, DeviceRecord, DeviceService>
{
    private readonly DeviceService _deviceService;
    private readonly CheqroomService _cheqroom;
    private readonly JamfService _jamf;
    
    private readonly WinsorDeviceStub _device;

    [ObservableProperty] private bool hasWinsorData;
    [ObservableProperty] private string assetTag = "";
    [ObservableProperty] private string categoryName = "";
    [ObservableProperty] private string cheqroomId = "";
    [ObservableProperty] private int jamfId;
    [ObservableProperty] private int jamfInventoryPreloadId;
    [ObservableProperty] private bool loaner;
    [ObservableProperty] private CategorySearchViewModel categorySearch = new();
    [ObservableProperty] private DateTime purchaseDate;
    [ObservableProperty] private double purchaseCost;
    [ObservableProperty] private JamfViewModel jamfDetails = IEmptyViewModel<JamfViewModel>.Empty;
    [ObservableProperty] private bool showJamf;
    [ObservableProperty] private InventoryPreloadViewModel jamfInventoryPreload = IEmptyViewModel<InventoryPreloadViewModel>.Empty;
    [ObservableProperty]
    private bool showInventoryPreload;
    [ObservableProperty] private CheqroomItemViewModel cheqroomItem = IEmptyViewModel<CheqroomItemViewModel>.Empty;
    [ObservableProperty]
    private bool showCheqroom;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<JamfViewModel>? JamfSelected;
    public event EventHandler<InventoryPreloadViewModel>? InventoryPreloadSelected;
    public event EventHandler<CheqroomItemViewModel>? CheqroomSelected;

    public DeviceCategoryViewModel Category
    {
        get => CategorySearch.Selected;
        set => CategorySearch.Select(value);
    }

    public static ConcurrentBag<WinsorDeviceViewModel> ViewModelCache { get; private set; } = [];

    public WinsorDeviceViewModel()
    {
        _deviceService = ServiceHelper.GetService<DeviceService>();
        _cheqroom = ServiceHelper.GetService<CheqroomService>();
        _jamf = ServiceHelper.GetService<JamfService>();
        CategorySearch.Select(new(_deviceService.Categories.First()));
        hasWinsorData = false;
        _device = new();
        assetTag = _device.assetTag;
        categoryName = _device.category;
        cheqroomId = _device.cheqroomId;
        jamfId = _device.jamfId;
        jamfInventoryPreloadId = _device.jamfInventoryPreloadId;
        loaner = _device.loaner;
    }
    


    private WinsorDeviceViewModel(DeviceRecord dev)
    {
        using (DebugTimer _ = new($"Getting WinsorDevice ViewModel for {dev.winsorDevice?.assetTag ?? "Missing Asset Tag?"}", ServiceHelper.GetService<LocalLoggingService>()))
        {
            _deviceService = ServiceHelper.GetService<DeviceService>()!;
            _cheqroom = ServiceHelper.GetService<CheqroomService>();
            _jamf = ServiceHelper.GetService<JamfService>();
            hasWinsorData = dev.isWinsorDevice;
            _device = dev.winsorDevice ?? WinsorDeviceStub.Default;
            assetTag = _device.assetTag ?? "";
            categoryName = _device.category ?? "";
            cheqroomId = _device.cheqroomId ?? "";
            jamfId = _device.jamfId;
            jamfInventoryPreloadId = _device.jamfInventoryPreloadId;
            loaner = _device.loaner;
            if(!string.IsNullOrEmpty(_device.category))
                CategorySearch.Select(new(_device.category));
            if (dev.winsorDevice.HasValue)
            {
                var task = _deviceService.GetWinsorDeviceDetails(dev.id, OnError.DefaultBehavior(this));
                task.WhenCompleted(() =>
                {
                    if (!task.Result.HasValue) return;
                    var details = task.Result!.Value;
                    PurchaseDate = details.purchaseDate;
                    PurchaseCost = details.purchaseCost;
                    if (details.jamfId > 0)
                    {
                        LoadJamfDetails($"{details.jamfId}").SafeFireAndForget(e => e.LogException());
                    }
                });
                task.SafeFireAndForget(e => e.LogException());
            }
        }
    }

    private async Task LoadJamfDetails(string jamfId)
    {
        if(Category.JamfDeviceType == "Computer")
        {
            var computer = await _jamf.GetComputerDetails(jamfId, OnError.DefaultBehavior(this));
            if (computer.HasValue)
                JamfDetails = new(computer.Value);
        }
        else
        {
            var device = await _jamf.GetMobileDeviceDetails(jamfId, OnError.DefaultBehavior(this));
            if (device.HasValue)
                JamfDetails = new(device.Value);
        }

        ShowJamf = !string.IsNullOrEmpty(JamfDetails.Id);

        if (ShowJamf)
        {
            JamfDetails.OnError += OnError.PassAlong();
            JamfDetails.Selected += JamfSelected.PassAlong();
        }
    }

    private async Task LoadInventoryPreload(string id)
    {
        var preload = await _jamf.GetInventoryPreload(id, OnError.DefaultBehavior(this));
        ShowInventoryPreload = preload.HasValue;

        if (ShowInventoryPreload)
        {
            JamfInventoryPreload = new(preload.Value);
            JamfInventoryPreload.OnError += OnError.PassAlong();
            JamfInventoryPreload.Selected += InventoryPreloadSelected.PassAlong();
        }
    }

    private async Task LoadCheqroom(string id)
    {
        var item = await _cheqroom.GetItem(id, OnError.DefaultBehavior(this));
        ShowCheqroom = item.HasValue;
        if(ShowCheqroom)
        {
            CheqroomItem = CheqroomItemViewModel.Get(item.Value);
        }
    }


    public UpdateWinsorDeviceRecord GetUpdateRecord() =>
        new(Category.Id, PurchaseDate, PurchaseCost);

    public CreateWinsorDeviceRecord GetCreateRecord() =>
        new(Category.Id, PurchaseDate, PurchaseCost);

    public static List<WinsorDeviceViewModel> GetClonedViewModels(IEnumerable<DeviceRecord> models)
    {
        List<WinsorDeviceViewModel> output = [];
        foreach(var  model in models)
        {
            if (!model.winsorDevice.HasValue) 
                continue;

            var vm = ViewModelCache.FirstOrDefault(dev => dev.AssetTag == model.winsorDevice!.Value.assetTag);
            if(vm is null)
            {
                vm = new WinsorDeviceViewModel(model);
                ViewModelCache.Add(vm);
            }

            output.Add(vm.Clone());
        }
        if(output.Count == 0)
            output.Add(IEmptyViewModel<WinsorDeviceViewModel>.Empty);
        return output;
    }

    public static async Task Initialize(DeviceService service, ErrorAction onError)
    {
        while (!service.Ready)
            await Task.Delay(250);
        ViewModelCache = [..
            service.DeviceCache.Where(dev => dev.winsorDevice.HasValue)
            .Select(dev => new WinsorDeviceViewModel(dev))];
            
    }

    public static WinsorDeviceViewModel Get(DeviceRecord model) => GetClonedViewModels([model])[0];

    public WinsorDeviceViewModel Clone() => (WinsorDeviceViewModel)this.MemberwiseClone();
}