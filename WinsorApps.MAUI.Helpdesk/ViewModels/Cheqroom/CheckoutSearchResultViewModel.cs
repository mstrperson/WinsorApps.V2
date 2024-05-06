using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom
{
    public partial class CheckoutSearchResultViewModel : ObservableObject, IErrorHandling, IEmptyViewModel<CheckoutSearchResultViewModel>
    {
        private readonly CheqroomCheckoutSearchResult _searchResult;
        private readonly CheqroomService _cheqroom;

        [ObservableProperty] private ImmutableArray<string> items = [];
        [ObservableProperty] private UserViewModel user;
        [ObservableProperty] private DateTime created;
        [ObservableProperty] private DateTime due;
        [ObservableProperty] private string status;
        [ObservableProperty] private bool isOverdue;

        [ObservableProperty] private string[] style;

        public event EventHandler<ErrorRecord>? OnError;
        public event EventHandler<CheckoutSearchResultViewModel>? OnSelected;
        public event EventHandler<CheckoutSearchResultViewModel>? OnCheckedIn;

        public CheckoutSearchResultViewModel()
        {
            _searchResult = new();
            _cheqroom = ServiceHelper.GetService<CheqroomService>();
            user = IEmptyViewModel<UserViewModel>.Empty;
            style = [];
            status = "";
        }

        public CheckoutSearchResultViewModel(CheqroomCheckoutSearchResult result)
        {
            _searchResult = result;
            _cheqroom = ServiceHelper.GetService<CheqroomService>();
            user = new(result.user);
            created = result.created.LocalDateTime;
            items = result.items;
            due = result.due.LocalDateTime;
            status = result.status;
            isOverdue = result.isOverdue;
            style = IsOverdue ? ["Error"] : [];
        }


        [RelayCommand]
        public void Select()
        {
            OnSelected?.Invoke(this, this);
        }

        [RelayCommand]
        public async Task<bool> CheckIn()
        {
            bool success = true;
            await _cheqroom.CheckInItem(_searchResult._id,
                err => { success = false; OnError.DefaultBehavior(this)(err); });

            return success;
        }
    }
}
