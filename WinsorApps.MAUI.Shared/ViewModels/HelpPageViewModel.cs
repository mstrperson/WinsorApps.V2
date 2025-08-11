using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels
{
    public partial class HelpPageViewModel : 
        ObservableObject,
        IBusyViewModel
    {
        private readonly LocalLoggingService _logging;
        private readonly ApiService _api;
        private readonly AppService _app;
        private readonly RegistrarService _registrar;
        private readonly LoginViewModel _login;

        public event EventHandler<ErrorRecord>? OnError;

        public HelpPageViewModel(
            LocalLoggingService logging, 
            ApiService api, 
            AppService app, 
            RegistrarService registrar, 
            LoginViewModel login)
        {
            _logging = logging;
            _api = api;
            api.OnLoginSuccess += Api_OnLoginSuccess;
            _app = app;
            _registrar = registrar;
            _registrar.WaitForInit(OnError.DefaultBehavior(this)).WhenCompleted(() =>
            {
                MasqSearch.SetAvailableUsers(_registrar.EmployeeList);
                MasqSearch.OnSingleResult += (_, selected) =>
                    MasqSelection = selected;
            });
            _login = login;
        }

        private void Api_OnLoginSuccess(object? sender, EventArgs e)
        {
            LoggedInUser = UserViewModel.Get(_api.UserInfo!);
            var roleTask = _api.UserInfo?.GetRoles(_api);
            roleTask?.WhenCompleted(() =>
            {
                var roles = roleTask.Result;
                CanMasquerade = roles.Any(role => role.StartsWith("System Admin", StringComparison.InvariantCultureIgnoreCase));
            });

        }

        [ObservableProperty] private ObservableCollection<ServiceAwaiterViewModel> services = [];
        [ObservableProperty] private DateTime logStart = DateTime.Today.AddDays(-14);
        [ObservableProperty] private DateTime logEnd = DateTime.Today.AddDays(1);
        [ObservableProperty] private UserViewModel loggedInUser = UserViewModel.Empty;
        [ObservableProperty] private bool canMasquerade;
        [ObservableProperty] private bool isMasqing;
        [ObservableProperty] private UserSearchViewModel masqSearch = new();
        [ObservableProperty] private UserViewModel masqSelection = UserViewModel.Empty;
        [ObservableProperty] private bool busy;
        [ObservableProperty] private string busyMessage = "";

        public string StoragePath => _logging.AppStoragePath;
        public DateTime LastUpdated => _app.LastVersionUpdated;
        public string Architecture => _logging.ValidArchitecture;

        [RelayCommand]
        public async Task Masquerade()
        {
            Busy = true;
            BusyMessage = $"Masquerading as {MasqSelection.DisplayName}";
            if(!string.IsNullOrEmpty(MasqSelection.Id) &&
                await _api.Masquerade(MasqSelection.Id, OnError.DefaultBehavior(this)))
            {
                IsMasqing = true;
                LoggedInUser = UserViewModel.Get(_api.UserInfo!);
                foreach(var service in Services)
                {
                    service.Refresh().SafeFireAndForget(e => e.LogException());
                }
            }
            Busy = false;
        }

        [RelayCommand]
        public async Task Logout()
        {
            await _login.Logout();
            Application.Current?.Quit();
        }

        [RelayCommand]
        public async Task DropMasq()
        {
            Busy = true;
            BusyMessage = $"Dropping Masqerade as {MasqSelection.DisplayName}";
            await _api.DropMasq();
            IsMasqing = false;
            LoggedInUser = UserViewModel.Get(_api.UserInfo!);
            Busy = false;
        }
        
        [RelayCommand]
        public async Task SubmitLogs()
        {
            var logs = _logging.GetRecentLogs(LogStart, LogEnd);
            if (logs.Select(kvp => kvp.Value.Length).Sum() < 5_000_000) // if the logs are less than 5MiB in size...
            {
                await _api.SendAsync<Dictionary<string, byte[]>, string>(HttpMethod.Post, "api/users/self/submit-logs", logs, onError: OnError.DefaultBehavior(this));
                return;
            }

            List<Dictionary<string, byte[]>> chunks = [  ];

            while (logs.Count > 0)
            {
                var chunk = new Dictionary<string, byte[]>();
                while (chunk.Select(kvp => kvp.Value.Length).Sum() < 5_000_000 && logs.Count > 0)
                {
                    var log = logs.First();
                    logs.Remove(log.Key);
                    chunk.Add(log.Key, log.Value);
                }

                chunks.Add(chunk);
            }

            foreach (var chunk in chunks)
            {
                await _api.SendAsync<Dictionary<string, byte[]>, string>(HttpMethod.Post, "api/users/self/submit-logs", chunk, onError: OnError.DefaultBehavior(this));
            }
        }
    }
}
