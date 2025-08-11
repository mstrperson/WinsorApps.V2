using System.Text.Json;
using WinsorApps.Services.Global.Models;
using static WinsorApps.Services.Global.Services.LocalLoggingService;

namespace WinsorApps.Services.Global.Services
{
    public class AppService(ApiService api, LocalLoggingService logging) : 
        IAsyncInitService
    {
        private readonly ApiService _api = api;
        private readonly LocalLoggingService _logging = logging;

        public string CacheFileName => VersionFilePath;

        public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }
        public async Task SaveCache() => await Task.CompletedTask;
        public bool LoadCache() => true;
        public bool Allowed { get; private set; }

        public bool UpdateAvailable { get; private set; }

        public bool Started { get; private set; }

        public bool Ready { get; private set; }

        public double Progress { get; private set; }

        public string AppId { get; set; } = "";

        public AppInstallerGroup Group { get; private set; } = AppInstallerGroup.Default;

        public event EventHandler? AppNotAuthorized;

        private AppVersionList _versionList = new([]);

        public async Task<AppInstallerAvailableRoles> GetAllowedRoles(ErrorAction onError)
        {
            if (string.IsNullOrEmpty(AppId))
            {
                _logging.LogMessage(LogLevel.Error, "AppId is not set. Cannot get allowed roles.");
                return new("", "", []);
            }
            var result = await _api.SendAsync<AppInstallerAvailableRoles>(HttpMethod.Get,
                $"api/apps/{AppId}/roles", authorize: false, onError: onError);

            return result ?? new("", "", []);
        }

        public async Task<bool> AmIAllowed(ErrorAction onError)
        {
            if (string.IsNullOrEmpty(AppId))
            {
                _logging.LogMessage(LogLevel.Error, "AppId is not set. Cannot check if user is allowed.");
                return true;
            }
            var roles = await _api.UserInfo!.GetRoles(_api);
            if (roles.Contains("System Admin"))
                return true;

            var userId = _api.AuthUserId;
            if(string.IsNullOrEmpty(userId)) 
                return false;

            var allowedApps = await _api.SendAsync<List<AppInstallerGroup>>(HttpMethod.Get,
                $"api/apps/for/{userId}", authorize: false, onError: onError)
                ?? [];

            return allowedApps.Any(app => app.id == AppId);
        }

        public async Task<bool> CheckForUpdates()
        {
            if (string.IsNullOrEmpty(AppId))
            {
                _logging.LogMessage(LogLevel.Error, "AppId is not set. Cannot check for updates.");
                return false;
            }
            var app = await CheckAppStatus();
            if (app is null)
                return false;

            if (app.availableDownloads.Any(version =>
                version.arch == _logging.ValidArchitecture &&
                version.contentType == _logging.ValidExecutableType &&
                version.uploaded > LastVersionUpdated))
            {
                var version = app.availableDownloads.First(v =>
                                v.arch == _logging.ValidArchitecture &&
                                v.contentType == _logging.ValidExecutableType &&
                                v.uploaded > LastVersionUpdated);
                _logging.LogMessage(LogLevel.Debug,
                    $"Found version update: {version.uploaded:yyyy-MM-dd hh:mm tt} > {LastVersionUpdated:yyyy-MM-dd hh:mm tt}");
                return true;
            }

            return false;
        }

        public async Task CheckForUpdates(Action<FileStreamWrapper> onNewVersionAvailable, ErrorAction onError)
        {
            if (string.IsNullOrEmpty(AppId))
            {
                _logging.LogMessage(LogLevel.Error, "AppId is not set. Cannot check for updates.");
                return;
            }
            var app = await CheckAppStatus();
            if (app is null)
                return;

            if (app.availableDownloads.Any(version =>
                version.arch == _logging.ValidArchitecture &&
                version.contentType == _logging.ValidExecutableType &&
                version.uploaded > LastVersionUpdated))
            {
                var version = app.availableDownloads.First(v =>
                                v.arch == _logging.ValidArchitecture &&
                                v.contentType == _logging.ValidExecutableType &&
                                v.uploaded > LastVersionUpdated);
                _logging.LogMessage(LogLevel.Debug,
                    $"Found version update: {version.uploaded:yyyy-MM-dd hh:mm tt} > {LastVersionUpdated:yyyy-MM-dd hh:mm tt}");
                onNewVersionAvailable((await DownloadLatestVersion(onError))!);
            }
        }

        public async Task<List<AppInstallerGroup>> GetAllApps() =>
            await _api.SendAsync<List<AppInstallerGroup>?>(HttpMethod.Get,
                "api/apps", authorize: false) ?? [];

        public async Task<AppInstallerGroup?> CheckAppStatus() => 
            await _api.SendAsync<AppInstallerGroup?>(HttpMethod.Get,
                $"api/apps/{AppId}", authorize: false);

        public static string Arch => System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.Arm64 or
            System.Runtime.InteropServices.Architecture.Arm or
            System.Runtime.InteropServices.Architecture.Armv6 => "arm64",
            System.Runtime.InteropServices.Architecture.X64 or
            System.Runtime.InteropServices.Architecture.X86 => "x86",
            _ => ""
        };

        public static string Type => Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => "exe",
            _ => "pkg"
        };

        public string GetBrowserLinkForLatestVersion()
        {
            (var arch, var type) = (Arch, Type);

            var stub = Group.availableDownloads.FirstOrDefault(inst => inst.arch == arch && inst.contentType == type);

            if (stub == default)
                return "";

            var url = $"{_api.BaseAddress}/api/apps/{Group.id}/latest?arch={arch}&type={type}";


            return url;
        }

        public async Task<FileStreamWrapper?> DownloadLatestVersion(ErrorAction onError)
        {
            (var arch, var type) = (Arch, Type);
            var result = await _api.DownloadFileExt($"api/apps/{AppId}/latest?arch={arch}&type={type}", authorize: false,
                onError: onError);

            return result;          
        }

        public async Task Initialize(ErrorAction onError)
        {

            while (string.IsNullOrEmpty(AppId))
                await Task.Delay(250);

            Started = true;

            var success = false;
            if (File.Exists(VersionFilePath))
            {
                var versionFileJson = File.ReadAllText(VersionFilePath);
                try
                {
                    var list = JsonSerializer.Deserialize<AppVersionList>(versionFileJson);
                    if(list is not null)
                    {
                        _versionList = list;
                        success = true;
                    }
                }
                catch(Exception ex) 
                {
                    ex.LogException(_logging);
                    _versionList = new([]);
                }
            }

            if(!success)
            {
                _versionList.UpdateApp(AppId);
                _versionList_SaveRequested(this, EventArgs.Empty);
            }

            _versionList.SaveRequested += _versionList_SaveRequested;

            _ = _versionList[AppId];

            Allowed = await AmIAllowed(onError);
            UpdateAvailable = await CheckForUpdates();
            Group = await CheckAppStatus() ?? AppInstallerGroup.Default;
            Progress = 1;
            Ready = true;
        }

        private void _versionList_SaveRequested(object? sender, EventArgs e)
        {
            var newVersionFileJson = JsonSerializer.Serialize(_versionList);
            File.WriteAllText(VersionFilePath, newVersionFileJson);
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(100);
        }

        public Task Refresh(ErrorAction onError) => Task.CompletedTask;

        private string VersionFilePath => $"{AppDataPath}{Path.DirectorySeparatorChar}appVersionInstallDates.json";
        public DateTime LastVersionUpdated
        {
            get
            {
                var version = _versionList[AppId];
                return version.installedOn;
            }
            set
            {
                _versionList.UpdateApp(AppId);
            }
        }
    }
}
