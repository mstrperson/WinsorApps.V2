using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels
{
    public partial class HelpPageViewModel : ObservableObject
    {
        private readonly LocalLoggingService _logging;
        private readonly ApiService _api;

        public event EventHandler<ErrorRecord>? OnError;

        public HelpPageViewModel(LocalLoggingService logging, ApiService api)
        {
            _logging = logging;
            _api = api;
            api.OnLoginSuccess += Api_OnLoginSuccess;
        }

        private void Api_OnLoginSuccess(object? sender, EventArgs e)
        {
            LoggedInUser = UserViewModel.Get(_api.UserInfo!.Value);
        }

        [ObservableProperty] private ImmutableArray<ServiceAwaiterViewModel> services = [];
        [ObservableProperty] private DateTime logStart = DateTime.Today.AddDays(-14);
        [ObservableProperty] private DateTime logEnd = DateTime.Today.AddDays(1);
        [ObservableProperty] private UserViewModel loggedInUser = IEmptyViewModel<UserViewModel>.Empty;

        public string StoragePath => _logging.AppStoragePath;
        public DateTime LastUpdated => _logging.LastVersionUpdated;
        public string Architecture => _logging.ValidArchitecture;


        [RelayCommand]
        public async Task SubmitLogs()
        {
            var logs = _logging.GetRecentLogs(LogStart, LogEnd);
            await _api.SendAsync<Dictionary<string, byte[]>, string>(HttpMethod.Post, "api/users/self/submit-logs", logs, onError: OnError.DefaultBehavior(this));
        }
    }
}
