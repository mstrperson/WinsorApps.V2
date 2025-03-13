using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Helpdesk.Models;

public record CheqroomItemStatus(string id, string name, string status);

public record CheqroomCheckoutResult(string _id, string status, string itemSummary, DateTime due);
public record CheqroomCheckoutSearchResult(string _id, UserRecord user, List<string> items,
    DateTimeOffset created, DateTimeOffset due, string status, bool isOverdue);

public record CheqroomItem(string location,
    string status, string category, string name, string _id, string model, string brand, CheqroomItemFields fields, List<string> barcodes)
{
    public static CheqroomItem Default => new("", "", "", "", "", "", "", new("", "", ""), []);
    public string assetTag => barcodes.Count == 0 ? "" : barcodes[0];
}

public record CheqroomItemFields(string Owner, string OwnerId, string SerialNumber);