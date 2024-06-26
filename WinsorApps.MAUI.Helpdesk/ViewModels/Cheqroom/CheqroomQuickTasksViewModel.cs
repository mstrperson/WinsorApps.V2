using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom
{
    public partial class CheqroomQuickTasksViewModel : ObservableObject, IErrorHandling
    {
        private readonly CheqroomService _cheqroom;

        [ObservableProperty]
        private QuickCheckoutViewModel quickCheckout;

        [ObservableProperty]
        private CheckoutSearchViewModel checkoutSearch;

        [ObservableProperty]
        private bool showCheckout;

        [ObservableProperty]
        private bool showCheckin;

        [ObservableProperty]
        private string toggleLabel;

        public event EventHandler<ErrorRecord>? OnError;

        public CheqroomQuickTasksViewModel(CheqroomService cheqroom, QuickCheckoutViewModel quickCheckout, CheckoutSearchViewModel checkoutSearch)
        {
            _cheqroom = cheqroom;
            this.quickCheckout = quickCheckout;
            this.checkoutSearch = checkoutSearch;
            showCheckout = true;
            showCheckin = false;
            toggleLabel = "Quick Checkout";
        }

        [RelayCommand]
        public async Task Reset()
        {
            QuickCheckout.Clear();
            CheckoutSearch.Clear();
            await _cheqroom.Refresh(OnError.DefaultBehavior(this));
            
        }

        [RelayCommand]
        public void ToggleInOut()
        {
            ShowCheckout = !ShowCheckout;
            ShowCheckin = !ShowCheckin;
            ToggleLabel = ShowCheckin ? "Quick Check In" : "Quick Check Out";
        }
    }
}
