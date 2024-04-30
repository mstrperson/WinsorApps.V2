using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Global.Services
{
    public class AppService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;
        public AppService(ApiService api, LocalLoggingService logging)
        {
            _api = api;
            _logging = logging;
        }

        public async Task<AppInstallerAvailableRoles> GetAllowedRoles(string groupId, ErrorAction onError)
        {
            var result = await _api.SendAsync<AppInstallerAvailableRoles>(HttpMethod.Get,
                $"api/apps/{groupId}/roles", authorize: false, onError: onError);

            return result;
        }

        public async Task<bool> AmIAllowed(string groupId, ErrorAction onError)
        {
            var roles = await _api.UserInfo!.Value.GetRoles(_api);
            if (roles.Contains("System Admin"))
                return true;

            var userId = _api.AuthUserId;
            if(string.IsNullOrEmpty(userId)) 
                return false;

            var allowedApps = await _api.SendAsync<ImmutableArray<AppInstallerGroup>>(HttpMethod.Get,
                $"api/apps/for/{userId}", authorize: false, onError: onError);

            return allowedApps.Any(app => app.id == groupId);
        }

        public async Task<bool> CheckForUpdates(string groupId, ErrorAction onError)
        {
            var app = await CheckAppStatus(groupId);
            if (app == default)
                return false;

            if (app.availableDownloads.Any(version =>
                version.arch == _logging.ValidArchitecture &&
                version.contentType == _logging.ValidExecutableType &&
                version.uploaded > _logging.LastVersionUpdated))
            {
                var version = app.availableDownloads.First(v =>
                                v.arch == _logging.ValidArchitecture &&
                                v.contentType == _logging.ValidExecutableType &&
                                v.uploaded > _logging.LastVersionUpdated);
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug,
                    $"Found version update: {version.uploaded:yyyy-MM-dd hh:mm tt} > {_logging.LastVersionUpdated:yyyy-MM-dd hh:mm tt}");
                return true;
            }

            return false;
        }

        public async Task<string> CheckForUpdates(string groupId)
        {

            var app = await CheckAppStatus(groupId);
            if (app == default)
                return "";

            return OpenBrowserLinkForLatestVersion(app);
        }

        public async Task CheckForUpdates(string groupId, Action<FileStreamWrapper> onNewVersionAvailable, ErrorAction onError)
        {
            var app = await CheckAppStatus(groupId);
            if (app == default)
                return;

            if (app.availableDownloads.Any(version =>
                version.arch == _logging.ValidArchitecture &&
                version.contentType == _logging.ValidExecutableType &&
                version.uploaded > _logging.LastVersionUpdated))
            {
                var version = app.availableDownloads.First(v =>
                                v.arch == _logging.ValidArchitecture &&
                                v.contentType == _logging.ValidExecutableType &&
                                v.uploaded > _logging.LastVersionUpdated);
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug,
                    $"Found version update: {version.uploaded:yyyy-MM-dd hh:mm tt} > {_logging.LastVersionUpdated:yyyy-MM-dd hh:mm tt}");
                onNewVersionAvailable((await DownloadLatestVersion(groupId, onError))!);
            }
        }

        public async Task<ImmutableArray<AppInstallerGroup>> GetAllApps() =>
            await _api.SendAsync<ImmutableArray<AppInstallerGroup>>(HttpMethod.Get,
                "api/apps", authorize: false);

        public async Task<AppInstallerGroup> CheckAppStatus(string groupId) => 
            await _api.SendAsync<AppInstallerGroup>(HttpMethod.Get,
                $"api/apps/{groupId}", authorize: false);



        public string OpenBrowserLinkForLatestVersion(AppInstallerGroup group)
        {
            var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.Arm64 or
                System.Runtime.InteropServices.Architecture.Arm or
                System.Runtime.InteropServices.Architecture.Armv6 => "arm64",
                System.Runtime.InteropServices.Architecture.X64 or
                System.Runtime.InteropServices.Architecture.X86 => "x86",
                _ => ""
            };

            var type = Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => "exe",
                _ => "pkg"
            };

            var stub = group.availableDownloads.FirstOrDefault(inst => inst.arch == arch && inst.contentType == type);

            if (stub == default)
                return "";

            var url = $"{_api.BaseAddress}/api/apps/{group.id}/latest?arch={arch}&type={type}";


            return url;
        }

        public async Task<FileStreamWrapper?> DownloadLatestVersion(string groupId, ErrorAction onError)
        {
            var result = await _api.DownloadFileExt($"api/apps/{groupId}/latest", authorize: false,
                onError: onError);

            return result;          
        }
    }
}
