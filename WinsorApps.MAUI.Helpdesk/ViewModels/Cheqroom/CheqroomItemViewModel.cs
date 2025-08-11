using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;

public partial class CheqroomItemViewModel : 
    ObservableObject,
    IDefaultValueViewModel<CheqroomItemViewModel>,
    ICachedViewModel<CheqroomItemViewModel, CheqroomItem, CheqroomService>
{
    private readonly CheqroomService _cheqroom;
    private readonly CheqroomItem _item;

    [ObservableProperty] private string assetTag = "";
    [ObservableProperty] private string location = "";
    [ObservableProperty] private string status = "";
    [ObservableProperty] private string category = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string model = "";
    [ObservableProperty] private string brand = "";
    [ObservableProperty] private string owner = "";
    [ObservableProperty] private string ownerId = "";
    [ObservableProperty] private string serialNubmer = "";

    public CheqroomItemViewModel()
    {

        _cheqroom = ServiceHelper.GetService<CheqroomService>();
        _item = CheqroomItem.Default;
        LoadItem(_item);
    }

    private CheqroomItemViewModel(CheqroomItem item)
    {
        _cheqroom = ServiceHelper.GetService<CheqroomService>();
        _item = item;
        LoadItem(item);
    }

    public static List<CheqroomItemViewModel> ViewModelCache { get; private set; } = [];

    public static CheqroomItemViewModel Empty => new();

    public static CheqroomItemViewModel Get(CheqroomItem model)
    {
        var vm = ViewModelCache.FirstOrDefault(cvm => cvm.AssetTag == model.assetTag);
        if(vm is null)
        {
            vm = new(model);
            ViewModelCache.Add(vm);
        }

        return vm.Clone();
    }

    public static List<CheqroomItemViewModel> GetClonedViewModels(IEnumerable<CheqroomItem> models)
    {
        List<CheqroomItemViewModel> output = [];
        foreach (var model in models)
            output.Add(Get(model));

        return output;
    }

    public static async Task Initialize(CheqroomService service, ErrorAction onError)
    {
        while (!service.Ready)
            await Task.Delay(250);
    }

    public CheqroomItemViewModel Clone() => (CheqroomItemViewModel)this.MemberwiseClone();

    private void LoadItem(CheqroomItem item)
    {
        AssetTag = item.assetTag;
        Location = item.location;
        Status = item.status;
        Category = item.category;
        Name = item.name;
        Model = item.model;
        Brand = item.brand;
        Owner = item.fields.Owner;
        OwnerId = item.fields.OwnerId;
        SerialNubmer = item.fields.SerialNumber;
    }
}