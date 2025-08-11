namespace WinsorApps.Services.Global.Models;

public record NoteRecord(string note)
{
    public static implicit operator string(NoteRecord nr) => nr.note;
    public static implicit operator NoteRecord(string note) => new(note);
}
public record AppInstallerStub(string id, string contentType, string arch, DateTime uploaded);
public record AppInstaller(string id, string groupId, string appName, string contentType,
    string arch, byte[] fileContent, DateTime uploaded);
public record AppInstallerGroup(string id, string appName, List<AppInstallerStub> availableDownloads)
{
    public static readonly AppInstallerGroup Default = new("", "", []);
}

public record AppInstallerAvailableRoles(string id, string appName, List<string> allowedRoles);

public record AppVersionInstallDate(string appId, DateTime installedOn);


public class AppVersionList(List<AppVersionInstallDate> installedVersions)
{
    public List<AppVersionInstallDate> InstalledVersions { get; private set; } = installedVersions ?? [];

    public event EventHandler? SaveRequested;

    public AppVersionInstallDate this[string appId]
    {
        get
        {
            if (!InstalledVersions.Any(app => app.appId == appId))
            {
                InstalledVersions.Add(new(appId, DateTime.Now));  // This app wasn't in the list so it must be newly installed.
                SaveRequested?.Invoke(this, EventArgs.Empty);
            }

            return InstalledVersions.First(app => app.appId == appId);
        }
    }

    public void UpdateApp(string appId)
    {
        var app = this[appId];
        InstalledVersions.Replace(app, new(appId, DateTime.Now));
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }
}
