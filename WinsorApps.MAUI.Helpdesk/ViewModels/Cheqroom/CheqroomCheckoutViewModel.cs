using CommunityToolkit.Mvvm.ComponentModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels;

public partial class CheqroomCheckoutViewModel : ObservableObject
{
    private readonly CheqroomService _cheqroom;

    [ObservableProperty] private CheqroomItemViewModel item;
    [ObservableProperty] private UserViewModel user;

    public CheqroomCheckoutViewModel()
    {
        _cheqroom = ServiceHelper.GetService<CheqroomService>();
        
    }
}