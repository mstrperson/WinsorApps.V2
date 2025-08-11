using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Bookstore.Models;
public record SummerSection(string id, AdvisorRecord teacher, CourseRecord course, string schoolYear, DateTime submitted, List<SummerBookOrderListItem> books);

public record SummerBookOrderListItem(ISBNDetail isbn, int quantity);