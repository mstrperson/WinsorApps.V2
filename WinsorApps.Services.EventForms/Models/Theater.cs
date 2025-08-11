using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Models;
public record TheaterMenuCategory(string id, string name, List<TheaterMenuItem> items, bool deleted = false);
public record TheaterMenuItem(string id, string name, string categoryId, bool deleted = false);

public record NewTheaterEvent(string notes, List<string> itemIds);

public record TheaterEvent(string eventId, string notes, List<DocumentHeader> attachments, List<TheaterMenuItem> items)
{
    public static readonly TheaterEvent Empty = new("", "", [], []);
}
