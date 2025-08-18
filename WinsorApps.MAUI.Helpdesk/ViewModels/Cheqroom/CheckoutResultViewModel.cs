using CommunityToolkit.Mvvm.ComponentModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom
{
    public class CheckoutResultViewModel(CheqroomCheckoutResult result) : ObservableObject, IDefaultValueViewModel<CheckoutResultViewModel>
    {
        private readonly CheqroomCheckoutResult _result = result;

        public string Id => _result._id;
        public string Status => _result.status;
        public string ItemSummary => _result.itemSummary;
        public DateTime Due => _result.due;

        public static CheckoutResultViewModel Empty => new(new("", "", "", DateTime.Today));
    }
}
