using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom
{
    public partial class QuickCheckoutViewModel : ObservableObject, IErrorHandling
    {
        private readonly CheqroomService _cheqroom;
        private readonly LocalLoggingService _logging;

        public QuickCheckoutViewModel(CheqroomService cheqroom, LocalLoggingService logging)
        {
            _cheqroom = cheqroom;
            _logging = logging;
        }


        [ObservableProperty] private string assetTag = "";
        [ObservableProperty] private UserSearchViewModel userSearch = new();
        [ObservableProperty] private CheckoutResultViewModel result = CheckoutResultViewModel.Empty;
        [ObservableProperty] private bool displayResult;
        [ObservableProperty] private TimeSpan resultTimeout = TimeSpan.FromSeconds(15);
        [ObservableProperty] private bool working;

        public event EventHandler<ErrorRecord>? OnError;
        public event EventHandler<CheckoutSearchResultViewModel>? OnCheckoutSuccessful;

        [RelayCommand]
        public async Task Checkout()
        {
            if (string.IsNullOrEmpty(AssetTag))
            {
                OnError?.Invoke(this, new("Missing Information", "You must enter an Asset Tag."));
                return;
            }
            if (string.IsNullOrEmpty(UserSearch.Selected.Id))
            {
                OnError?.Invoke(this, new("Missing Information", $"Please select a person to check out {AssetTag} to."));
                return;
            }

            Working = true;
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Checking out {AssetTag} to {UserSearch.Selected.DisplayName}");
            var result = await _cheqroom.QuickCheckOutItem(AssetTag, UserSearch.Selected.Id, OnError.DefaultBehavior(this));

            if (!string.IsNullOrEmpty(result._id))
            {
                Result = new(result);
                CheqroomCheckoutSearchResult sres = new(result._id, UserSearch.Selected.Model.Reduce(UserRecord.Empty), [..result.itemSummary.Split(',')], DateTime.Now, result.due, result.status, false);

                OnCheckoutSuccessful?.Invoke(this, CheckoutSearchResultViewModel.Get(sres));
                DisplayResult = true;
                AssetTag = "";
                UserSearch.ClearSelection();
                TimeOutFireAndForget();
            }

            Working = false;
        }

        private void TimeOutFireAndForget()
        {
            var task = Task.Delay(ResultTimeout);
            task.WhenCompleted(() =>
            {
                if (DisplayResult)
                {
                    Clear();
                }
            });
            task.SafeFireAndForget(e => e.LogException(_logging));
        }

        [RelayCommand]
        public void Clear()
        {
            AssetTag = "";
            UserSearch.ClearSelection();
            Result = CheckoutResultViewModel.Empty;
            DisplayResult = false;
        }
    }
}
