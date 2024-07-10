using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom
{
    public partial class CheckoutSearchResultViewModel : 
        ObservableObject, 
        IErrorHandling,
        IDefaultValueViewModel<CheckoutSearchResultViewModel>,
        ICachedViewModel<CheckoutSearchResultViewModel, CheqroomCheckoutSearchResult, CheqroomService>
    {
        private readonly CheqroomCheckoutSearchResult _searchResult;
        private readonly CheqroomService _cheqroom;
        private static readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

        [ObservableProperty] private string id = "";
        [ObservableProperty] private ImmutableArray<string> items = [];
        [ObservableProperty] private UserViewModel user;
        [ObservableProperty] private DateTime created;
        [ObservableProperty] private DateTime due;
        [ObservableProperty] private string status;
        [ObservableProperty] private bool isOverdue;
        [ObservableProperty] private bool working;

        public string CreatedStr => $"{Created:ddd dd MMMM, hh:mm tt}";
        public string DueStr => $"{Due:ddd dd MMMM, hh:mm tt}";

        [ObservableProperty] private string[] style;

        public static ConcurrentBag<CheckoutSearchResultViewModel> ViewModelCache { get; private set; } = [];

        public static CheckoutSearchResultViewModel Default => new();

        public event EventHandler<ErrorRecord>? OnError;
        public event EventHandler<CheckoutSearchResultViewModel>? OnSelected;
        public event EventHandler<CheckoutSearchResultViewModel>? OnCheckedIn;

        public CheckoutSearchResultViewModel()
        {
            _searchResult = new();
            _cheqroom = ServiceHelper.GetService<CheqroomService>();
            user = UserViewModel.Default;
            style = [];
            status = "";
        }

        private CheckoutSearchResultViewModel(CheqroomCheckoutSearchResult result)
        {
            _searchResult = result;
            id = result._id;
            _cheqroom = ServiceHelper.GetService<CheqroomService>();
            user = UserViewModel.Get(result.user);
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
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Checking In {_searchResult.items.DelimeteredList(", ")} for {User.DisplayName}");
            Working = true;
            bool success = true;
            await _cheqroom.CheckInItem(Id,
                err => { success = false; OnError.DefaultBehavior(this)(err); });
            Working = false;
            Status = "closed";
            if(success)
                OnCheckedIn?.Invoke(this, this);
            return success;
        }

        public static List<CheckoutSearchResultViewModel> GetClonedViewModels(IEnumerable<CheqroomCheckoutSearchResult> models)
        {
            List<CheckoutSearchResultViewModel> output = [];
            foreach (var model in models)
                output.Add(Get(model));

            return output;
        }

        public static async Task Initialize(CheqroomService service, ErrorAction onError)
        {
            while (!service.Ready)
                await Task.Delay(250);
            ViewModelCache = [..
                service.OpenOrders.Select(checkout => new CheckoutSearchResultViewModel(checkout))];
        }

        public static CheckoutSearchResultViewModel Get(CheqroomCheckoutSearchResult model)
        {
            var vm = ViewModelCache.FirstOrDefault(cvm => cvm.Id == model._id);
            if(vm is null)
            {
                vm = new CheckoutSearchResultViewModel(model);
                ViewModelCache.Add(vm);
            }

            return vm.Clone();
        }

        public CheckoutSearchResultViewModel Clone() => (CheckoutSearchResultViewModel)this.MemberwiseClone();
    }
}
