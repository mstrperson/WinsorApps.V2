using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;

public partial class CheqroomItemViewModel : ObservableObject, IEmptyViewModel<CheqroomItemViewModel>
{
    private readonly CheqroomService _cheqroom;
    private CheqroomItem _item;

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

        _cheqroom = ServiceHelper.GetService<CheqroomService>()!;
        _item = CheqroomItem.Default;
        LoadItem(_item);
    }

    public CheqroomItemViewModel(CheqroomItem item)
    {
        _cheqroom = ServiceHelper.GetService<CheqroomService>()!;
        _item = item;
        LoadItem(item);
    }

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