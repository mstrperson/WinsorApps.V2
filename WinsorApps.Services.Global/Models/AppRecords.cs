using System.Collections.Immutable;

namespace WinsorApps.Services.Global.Models
{
    public readonly record struct NoteRecord(string note)
    {
        public static implicit operator string(NoteRecord nr) => nr.note;
        public static implicit operator NoteRecord(string note) => new NoteRecord(note);
    }
    public readonly record struct AppInstallerStub(string id, string contentType, string arch, DateTime uploaded);
    public readonly record struct AppInstaller(string id, string groupId, string appName, string contentType,
        string arch, byte[] fileContent, DateTime uploaded);
    public readonly record struct AppInstallerGroup(string id, string appName, ImmutableArray<AppInstallerStub> availableDownloads);

    public readonly record struct AppInstallerAvailableRoles(string id, string appName, ImmutableArray<string> allowedRoles);
}
