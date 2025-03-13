using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom
{
    public partial class CheqroomQuickTasksViewModel(CheqroomService cheqroom, QuickCheckoutViewModel quickCheckout, CheckoutSearchViewModel checkoutSearch) : ObservableObject, IErrorHandling
    {
        private readonly CheqroomService _cheqroom = cheqroom;

        [ObservableProperty]
        private QuickCheckoutViewModel quickCheckout = quickCheckout;

        [ObservableProperty]
        private CheckoutSearchViewModel checkoutSearch = checkoutSearch;

        [ObservableProperty]
        private bool showCheckout = true;

        [ObservableProperty]
        private bool showCheckin = false;

        [ObservableProperty]
        private string toggleLabel = "Quick Checkout";

        public event EventHandler<ErrorRecord>? OnError;

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
