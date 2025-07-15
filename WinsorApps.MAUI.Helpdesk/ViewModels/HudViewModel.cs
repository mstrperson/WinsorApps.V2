using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Helpdesk.Pages;
using WinsorApps.MAUI.Helpdesk.Pages.Devices;
using WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;
using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Pages;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels
{
    public partial class HudViewModel : 
        ObservableObject, 
        IErrorHandling
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

            OpenCases = [..ServiceCaseViewModel.GetClonedViewModels(_caseService.OpenCases)];

            foreach(var serviceCase in OpenCases)
            {
                serviceCase.OnError += (sender, e) => OnError?.Invoke(sender, e);
                serviceCase.Selected += (sender, serviceCase) => OnCaseSelected?.Invoke(sender, serviceCase);
                serviceCase.OnUpdate += async (_, _) => await Refresh();
                serviceCase.OnClose += (_, _) => 
                    OpenCases.Remove(serviceCase);
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

        [ObservableProperty] private ObservableCollection<ServiceCaseViewModel> openCases = [];
        [ObservableProperty] private QuickCheckoutViewModel quickCheckout;
        [ObservableProperty] private CheckoutSearchViewModel checkoutSearch;
        [ObservableProperty] private bool hasOpenCases;
        [ObservableProperty] private bool loading;

        [RelayCommand]
        public async Task Refresh()
        {
            Loading = true;

            while (_caseService.Refreshing)
                await Task.Delay(100);

            OpenCases = [..ServiceCaseViewModel.GetClonedViewModels(_caseService.OpenCases)];

            foreach (var serviceCase in OpenCases)
            {
                serviceCase.OnError += (sender, e) => OnError?.Invoke(sender, e);
                serviceCase.Selected += (sender, serviceCase) => OnCaseSelected?.Invoke(sender, serviceCase);
                serviceCase.OnUpdate += async (_, _) => await Refresh();
                serviceCase.ShowNotifyButton = serviceCase.Status.Status.Contains("Ready");
                serviceCase.OnClose += (_, _) => 
                    OpenCases.Remove(serviceCase);
            }

           await _cheqroom.Refresh(OnError.DefaultBehavior(this));

            Loading = false;
        }

        [RelayCommand]
        public void StartServiceCase()
        {
            var dsvm = new DeviceSearchViewModel();
            dsvm.OnSingleResult += ServiceCaseDeviceSelected;
            dsvm.OnZeroResults += Dsvm_OnZeroResults;
            dsvm.OnError += (sender, err) => OnError?.Invoke(sender, err);
            var devicePage = new DeviceSearchPage(dsvm);
            PageRequested?.Invoke(this, devicePage);
        }

        private void Dsvm_OnZeroResults(object? sender, EventArgs e)
        {
            if (sender is not DeviceSearchViewModel dsvm)
                return;
            var dvm = new DeviceViewModel()
            {
                DisplayName = dsvm.SearchText
            };

            dvm.OnError += (sender, err) => OnError?.Invoke(sender, err);
            dvm.ChangesSaved += ServiceCaseDeviceSelected;

            var deviceEditorPage = new DeviceEditor(dvm);
            PageRequested?.Invoke(this, deviceEditorPage);
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
