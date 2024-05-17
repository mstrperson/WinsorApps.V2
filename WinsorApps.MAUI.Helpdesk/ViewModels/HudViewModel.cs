using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels
{
    public partial class HudViewModel : ObservableObject, IErrorHandling
    {
        private readonly ServiceCaseService _caseService;
        private readonly DeviceService _devices;
        private readonly CheqroomService _cheqroom;

        public event EventHandler<ErrorRecord>? OnError;

        public HudViewModel(CheqroomService cheqroom, DeviceService devices, ServiceCaseService caseService, CheckoutSearchViewModel checkoutSearch, QuickCheckoutViewModel quickCheckout)
        {
            _cheqroom = cheqroom;
            _devices = devices;
            _caseService = caseService;

            OpenCases = ServiceCaseViewModel.GetClonedViewModels(_caseService.OpenCases).ToImmutableArray();

            this.checkoutSearch = checkoutSearch;
            this.quickCheckout = quickCheckout;
        }

        [ObservableProperty] private ImmutableArray<ServiceCaseViewModel> openCases = [];
        [ObservableProperty] private QuickCheckoutViewModel quickCheckout;
        [ObservableProperty] private CheckoutSearchViewModel checkoutSearch;
        [ObservableProperty] private bool hasOpenCases;
        [ObservableProperty] private bool loading;

        [RelayCommand]
        public async Task Refresh()
        {
            Loading = true;
            await Task.WhenAll(
                _caseService.Refresh(OnError.DefaultBehavior(this)),
                _devices.Refresh(OnError.DefaultBehavior(this)),
                _cheqroom.Refresh(OnError.DefaultBehavior(this)));

            OpenCases = ServiceCaseViewModel.GetClonedViewModels(_caseService.OpenCases).ToImmutableArray();
            Loading = false;
        }
    } 
}
