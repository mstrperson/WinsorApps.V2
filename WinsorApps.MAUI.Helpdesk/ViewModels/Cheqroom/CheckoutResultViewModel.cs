using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom
{
    public class CheckoutResultViewModel : ObservableObject, IEmptyViewModel<CheckoutResultViewModel>
    {
        private readonly CheqroomCheckoutResult _result;

        public string Id => _result._id;
        public string Status => _result.status;
        public string ItemSummary => _result.itemSummary;
        public DateTime Due => _result.due;

        public CheckoutResultViewModel() => _result = new();

        public CheckoutResultViewModel(CheqroomCheckoutResult result)
        {
            _result = result;
        }
    }
}
