using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Helpdesk.Models;

public readonly record struct CheqroomItemStatus(string id, string name, string status);

public readonly record struct CheqroomCheckoutResult(string _id, string status, string itemSummary, DateTime due);
public readonly record struct CheqroomCheckoutSearchResult(string _id, UserRecord user, ImmutableArray<string> items,
    DateTimeOffset created, DateTimeOffset due, string status, bool isOverdue);

public readonly record struct CheqroomItem(string location,
    string status, string category, string name, string _id, string model, string brand, CheqroomItemFields fields, ImmutableArray<string> barcodes)
{
    public static CheqroomItem Default => new("", "", "", "", "", "", "", new("", "", ""), []);
    public string assetTag => barcodes.Length == 0 ? "" : barcodes[0];
}

public readonly record struct CheqroomItemFields(string Owner, string OwnerId, string SerialNumber);