using System.Collections.Immutable;
using System.Text.Json.Serialization;

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

[JsonSerializable(typeof(AppVersionList))]
public class AppVersionList
{
    [JsonPropertyName("installedVersions")]
    public ImmutableArray<AppVersionInstallDate> InstalledVersions { get; private set; } = [];

    [JsonConstructor]
    public AppVersionList(ImmutableArray<AppVersionInstallDate> installedVersions)
    {
        InstalledVersions = installedVersions;
    }

    public event EventHandler? SaveRequested;

    [JsonIgnore]
    public AppVersionInstallDate this[string appId]
    {
        get
        {
            if (!InstalledVersions.Any(app => app.appId == appId))
            {
                InstalledVersions = InstalledVersions.Add(new(appId, DateTime.Now));  // This app wasn't in the list so it must be newly installed.
                SaveRequested?.Invoke(this, EventArgs.Empty);
            }

            return InstalledVersions.First(app => app.appId == appId);
        }
    }

    public void UpdateApp(string appId)
    {
        var app = this[appId];
        InstalledVersions = InstalledVersions.Replace(app, new(appId, DateTime.Now));
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }
}
