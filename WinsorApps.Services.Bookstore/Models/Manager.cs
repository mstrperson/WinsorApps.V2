using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Bookstore.Models;

public readonly record struct TeacherBookOrderCollection(UserRecord teacher, 
    ImmutableArray<TeacherBookOrderDetail> orders)
{
    public bool Fall => orders.Any(ord => ord.Fall);
    public bool Spring => orders.Any(ord => ord.Spring);
}

public readonly record struct BookOrderReportEntry(
    UserRecord teacher, ProtoSection protoSection, ImmutableArray<TeacherBookRequest> books);
