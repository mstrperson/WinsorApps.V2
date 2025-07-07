using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Devices;

public partial class DeviceEditorViewModel :
    ObservableObject
{
    private readonly DeviceService _service = ServiceHelper.GetService<DeviceService>();

    [ObservableProperty] private DeviceViewModel device = DeviceViewModel.Empty;

}
