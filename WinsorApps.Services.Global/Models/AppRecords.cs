using System.Collections.Immutable;

namespace WinsorApps.Services.Global.Models;

public readonly record struct NoteRecord(string note)
{
    public static implicit operator string(NoteRecord nr) => nr.note;
    public static implicit operator NoteRecord(string note) => new NoteRecord(note);
}
public readonly record struct AppInstallerStub(string id, string contentType, string arch, DateTime uploaded);
public readonly record struct AppInstaller(string id, string groupId, string appName, string contentType,
    string arch, byte[] fileContent, DateTime uploaded);
public readonly record struct AppInstallerGroup(string id, string appName, ImmutableArray<AppInstallerStub> availableDownloads)
{
    public static readonly AppInstallerGroup Default = new("", "", []);
}

public readonly record struct AppInstallerAvailableRoles(string id, string appName, ImmutableArray<string> allowedRoles);

public readonly record struct AppVersionInstallDate(string appId, DateTime installedOn);

public class AppVersionList(ImmutableArray<AppVersionInstallDate> installedVersions)
{
    public AppVersionInstallDate this[string appId]
    {
        get
        {
            if (!installedVersions.Any(app => app.appId == appId))
                installedVersions = installedVersions.Add(new(appId, DateTime.Now));  // This app wasn't in the list so it must be newly installed.

            return installedVersions.First(app => app.appId == appId);
        }
    }

    public void UpdateApp(string appId)
    {
        var app = this[appId];
        installedVersions = installedVersions.Replace(app, new(appId, DateTime.Now));
    }
}
