using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Helpdesk.Models;
public readonly record struct ServiceCaseHistory(ImmutableArray<ServiceCase> serviceCases, double lifetimeRepairCost);

    public readonly record struct ServiceCaseNoteRecord(string notes, string from);
    public readonly record struct ServiceCase(
        string id, 
        DeviceRecord device, 
        UserRecord owner, 
        ImmutableArray<string> commonIssues,
        string intakeNotes, 
        DateTime opened, 
        DateTime? closed, 
        string status, 
        ImmutableArray<ServiceCaseNoteRecord> additionalNotes,
        ImmutableArray<DocumentHeader> attachedDocuments, 
        double repairCost = 0, 
        string loaner = "")
{
    public ServiceCase AddLoaner(string assetTag) => new(id, device, owner, commonIssues, intakeNotes, opened, closed, status, additionalNotes, attachedDocuments, repairCost, assetTag);
}

    public readonly record struct ServiceStatus(string id, string text, string description, string defaultNextId, bool isClosed)
    {
        public override string ToString() => text;

        /*public static implicit operator ServiceStatus?(string str)
        {
            if(string.IsNullOrWhiteSpace(str))
                return null;

            if (!ServiceCaseService.Ready)
                throw new ServiceNotReadyException("Unable to retrieve ServiceStatus from string yet.");

            if (ServiceCaseService.ServiceStatuses.All(status => status.id != str && status.text != str))
                return null;

            return ServiceCaseService.ServiceStatuses.First(status => status.id == str || status.text == str);
        }*/
    }

    public readonly record struct ServiceCaseCommonIssue(string id, string status, string description)
    {
        public override string ToString() => status;
    }

    /// <summary>
    /// Use for creating new Service Cases, Id's should be pulled from the appropriate endpoints.
    /// </summary>
    /// <param name="deviceId">The computer in question</param>
    /// <param name="commonIssueIds">Ids for selected common issues</param>
    /// <param name="intakeNotes">Notes for this case</param>
    /// <param name="openingStatusId">Id of the selected opening status</param>
    public readonly record struct NewServiceCase(string deviceId, ImmutableArray<string> commonIssueIds, string intakeNotes, string openingStatusId);
    public readonly record struct UpdateServiceCase(string caseId, string? statusId = null, string? notes = null, ImmutableArray<string>? commonIssueIds = null);

    public record ServiceCaseFilter(bool? open = null, string? status = null, string? deviceId = null, string? ownerId = null,
        DateTime start = default, DateTime end = default)
    {
        public string QueryString
        {
            get
            {
                List<string> queryParams = new List<string>();
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