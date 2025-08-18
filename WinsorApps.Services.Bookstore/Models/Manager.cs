using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Bookstore.Models;

public record TeacherBookOrderCollection(UserRecord teacher, 
    List<TeacherBookOrderDetail> orders)
{
    public bool Fall => orders.Any(ord => ord.Fall);
    public bool Spring => orders.Any(ord => ord.Spring);
}

public record BookOrderReportEntry(
    UserRecord teacher, ProtoSection protoSection, List<TeacherBookRequest> books);
