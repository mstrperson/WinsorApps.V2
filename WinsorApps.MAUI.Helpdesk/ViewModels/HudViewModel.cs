using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Helpdesk.Pages;
using WinsorApps.MAUI.Helpdesk.Pages.Devices;
using WinsorApps.MAUI.Helpdesk.Pages.ServiceCase;
using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
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
        public event EventHandler<ServiceCaseViewModel>? OnCaseSelected;
        public event EventHandler<ContentPage>? PageRequested;
        public event EventHandler? PopStackRequested;

        public HudViewModel(
            CheqroomService cheqroom, 
            DeviceService devices, 
            ServiceCaseService caseService, 
            CheckoutSearchViewModel checkoutSearch, 
            QuickCheckoutViewModel quickCheckout)
        {
            _cheqroom = cheqroom;
            _devices = devices;
            _caseService = caseService;

            OpenCases = ServiceCaseViewModel.GetClonedViewModels(_caseService.OpenCases).ToImmutableArray();

            foreach(var serviceCase in OpenCases)
            {
                serviceCase.OnError += (sender, e) => OnError?.Invoke(sender, e);
                serviceCase.Selected += (sender, serviceCase) => OnCaseSelected?.Invoke(sender, serviceCase);
                serviceCase.OnUpdate += async (_, _) => await Refresh();
            }

            this.checkoutSearch = checkoutSearch;
            checkoutSearch.OnError += (sender, e) => OnError?.Invoke(sender, e);
            checkoutSearch.OnZeroResults += CheckoutSearch_OnZeroResults;
            CheckoutSearch.OnCheckedIn += ShowCheckoutResult;
            this.quickCheckout = quickCheckout;
            quickCheckout.OnError += (sender, e) => OnError?.Invoke(sender, e);
            quickCheckout.OnCheckoutSuccessful += ShowCheckoutResult;

            PeriodicallyRefresh(CancellationToken.None).SafeFireAndForget(e => e.LogException());
        }

        private async Task PeriodicallyRefresh(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                await Refresh();
            }
        }

        private void CheckoutSearch_OnZeroResults(object? sender, EventArgs e)
        {
            SplashPageViewModel spvm = new("No Open Checkout Found", [$"{CheckoutSearch.SearchText} does not appear to be currently checked out."]);
            SplashPage page = new() { BindingContext = spvm };

            PageRequested?.Invoke(this, page);
        }

        private void ShowCheckoutResult(object? sender, CheckoutSearchResultViewModel e)
        {
            Loading = true;
            CheckoutSearch.Refresh().WhenCompleted(() =>
            {
                var page = new CheckoutResultPage() { BindingContext = e };
                PageRequested?.Invoke(this, page);
                Loading = false;
            });
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

            foreach (var serviceCase in OpenCases)
            {
                serviceCase.OnError += (sender, e) => OnError?.Invoke(sender, e);
                serviceCase.Selected += (sender, serviceCase) => OnCaseSelected?.Invoke(sender, serviceCase);
                serviceCase.OnUpdate += async (_, _) => await Refresh();
                serviceCase.ShowNotifyButton = serviceCase.Status.Status.Contains("Ready");
            }

            Loading = false;
        }

        [RelayCommand]
        public void StartServiceCase()
        {
            var devicePage = new DeviceSearchPage();
            devicePage.ViewModel.OnSingleResult += ServiceCaseDeviceSelected;
            PageRequested?.Invoke(this, devicePage);
        }

        private void ServiceCaseDeviceSelected(object? sender, Devices.DeviceViewModel e)
        {
            ServiceCaseViewModel vm = new()
            {
                Device = e
            };

            vm.OnCreate += async (sender, e) =>
            {
                await this.Refresh();
                PopStackRequested?.Invoke(this, EventArgs.Empty);
            };
            OnCaseSelected?.Invoke(this, vm);
        }

    } 
}
