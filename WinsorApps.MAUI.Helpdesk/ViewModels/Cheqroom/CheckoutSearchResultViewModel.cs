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

        [ObservableProperty] private ImmutableArray<CheqroomItemViewModel> items = [];
        [ObservableProperty] private UserViewModel user;
        [ObservableProperty] private DateTime created;
        [ObservableProperty] private DateTime due;
        [ObservableProperty] private string status;
        [ObservableProperty] private bool isOverdue;

        public event EventHandler<ErrorRecord>? OnError;

        public CheckoutSearchResultViewModel()
        {
            _searchResult = new();
            _cheqroom = ServiceHelper.GetService<CheqroomService>();
            user = IEmptyViewModel<UserViewModel>.Empty;
            status = "";
        }

        public CheckoutSearchResultViewModel(CheqroomCheckoutSearchResult result)
        {
            _searchResult = result;
            _cheqroom = ServiceHelper.GetService<CheqroomService>();
            user = new(result.user);
            created = result.created.LocalDateTime;
            due = result.due.LocalDateTime;
            status = result.status;
            isOverdue = result.isOverdue;
            GetItems().SafeFireAndForget(e => e.LogException(ServiceHelper.GetService<LocalLoggingService>())); 
        }

        private async Task GetItems()
        {
            var tasks = _searchResult.items.Select(id => _cheqroom.GetItem(id, Err));

            await Task.WhenAll(tasks);
       
            Items = tasks
                .Where(item => item.Result.HasValue)
                .Select(item => new CheqroomItemViewModel(item.Result!.Value))
                .ToImmutableArray();
        }

        private void Err(ErrorRecord e) => OnError?.Invoke(this, e);

        [RelayCommand]
        public async Task CheckIn()
        {
            await _cheqroom.CheckInItem(_searchResult._id, Err);
        }
    }
}
