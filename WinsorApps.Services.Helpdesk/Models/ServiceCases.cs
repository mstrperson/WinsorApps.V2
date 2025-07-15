using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Helpdesk.Models;
public record ServiceCaseHistory(List<ServiceCase> serviceCases, double lifetimeRepairCost);

public record ServiceCaseNoteRecord(string notes, string from);
public record ServiceCase(
    string id,
    DeviceRecord device,
    UserRecord owner,
    List<string> commonIssues,
    string intakeNotes,
    DateTime opened,
    DateTime? closed,
    string status,
    List<ServiceCaseNoteRecord> additionalNotes,
    List<DocumentHeader> attachedDocuments,
    bool waitingForFMM,
    bool disabledFMM,
    bool backupNeeded,
    bool backupCompleted,
    double repairCost = 0,
    string loaner = "")
{
    public ServiceCase AddLoaner(string assetTag) => this with { loaner = assetTag };

    public static ServiceCase Empty => new("", DeviceRecord.Empty, UserRecord.Empty, [], "", DateTime.Now, null, "", [], [], false, false, false, false, 0, "");
}

public record ServiceStatus(string id, string text, string description, string defaultNextId, bool isClosed)
{
    public override string ToString() => text;

}

public record ServiceCaseCommonIssue(string id, string status, string description)
{
    public override string ToString() => status;
}

public record NewServiceCase(
    string deviceId,
    List<string> commonIssueIds,
    string intakeNotes,
    string openingStatusId,
    bool waitingForFMM,
    bool disabledFMM,
    bool backupNeeded,
    bool backupCompleted);
public record UpdateServiceCase(
    string caseId,
    string? statusId = null,
    string? notes = null,
    List<string>? commonIssueIds = null,
    bool? waitingForFMM = null,
    bool? disabledFMM = null,
    bool? backupNeeded = null,
    bool? backupCompleted = null);

public record ServiceCaseFilter(bool? open = null, string? status = null, string? deviceId = null, string? ownerId = null,
DateTime start = default, DateTime end = default)
{
    public string QueryString
    {
        get
        {
            List<string> queryParams = [];
            if (open is not null) queryParams.Add($"open={open}");
            if (status is not null) queryParams.Add($"status={status}");
            if (deviceId is not null) queryParams.Add($"deviceId={deviceId}");
            if (ownerId is not null) queryParams.Add($"ownerId={ownerId}");
            if (start != default) queryParams.Add($"start={start:yyyy-MM-dd}");
            if (end != default) queryParams.Add($"end={end:yyyy-MM-dd}");

            return queryParams.Aggregate((a, b) => $"{a}&{b}");
        }
    }
}