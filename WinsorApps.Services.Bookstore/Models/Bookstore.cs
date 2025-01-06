using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Bookstore.Models;

public readonly record struct OrderStatus(string id, string label, string description);

public readonly record struct OrderOption(string id, string label, string description)
{
//    public static OrderOption? FromString(string label, TeacherBookstoreService bookstoreService)
//        => bookstoreService.OrderOptions.FirstOrDefault(opt => opt.label == label);
}

/// <summary>
/// Section Record used for Book Orders when Teachers are placing order requests.
/// </summary>
/// <param name="id">Id used to retrieve this protoSection</param>
/// <param name="course">Course information attached to this section</param>
/// <param name="schoolYearId">What School Year is this in</param>
/// <param name="teacherId">UserId for the teacher of this section.</param>
/// <param name="createdTimeStamp">When was this record created.</param>
public readonly record struct ProtoSection(
    string id,
    CourseRecord course,
    string schoolYearId,
    string teacherId,
    DateTime createdTimeStamp);

/// <summary>
/// A particular ISBN has been requested
/// </summary>
/// <param name="isbn">The ISBN requested</param>
/// <param name="submitted">When was this request submitted.</param>
/// <param name="quantity">how many of this book should be purchased.</param>
/// <param name="fall">Order this book for the fall term</param>
/// <param name="spring">Order this book for the spring term</param>
/// <param name="status">where in the order process is this order</param>
public readonly record struct TeacherBookRequest(
    string isbn,
    DateTime submitted,
    int quantity,
    bool fall,
    bool spring,
    string status,
    string groupId = "");

/// <summary>
/// A group of requests for the same book in multiple forms
/// </summary>
/// <param name="groupId">ID to reference the group as a whole</param>
/// <param name="option">Should the student purchase all of these versions, or only one, or is it optional all together.</param>
/// <param name="requestedISBNs">Records for each ISBN in this group</param>
public readonly record struct TeacherBookRequestGroup(
    string groupId,
    string option,
    ImmutableArray<TeacherBookRequest> requestedISBNs);

/// <summary>
/// All the book orders for a particular section
/// </summary>
/// <param name="protoSectionId">ID of the section this is connected to.</param>
/// <param name="books">Groups of Book orders.</param>
public readonly record struct TeacherBookOrder(
    string protoSectionId,
    string schoolYearId,
    DateTime created,
    ImmutableArray<TeacherBookRequest> books)
{
    public int Quantity => books.Any() ? books.First().quantity : 0;
    public bool Fall => books.Any(req => req.fall);
    public bool Spring => books.Any(req => req.spring);
}

public readonly record struct TeacherBookOrderDetail(
    ProtoSection section,
    ImmutableArray<TeacherBookRequest> books)
{
    public bool Fall => books.Any(req => req.fall);
    public bool Spring => books.Any(req => req.spring);

    public override string ToString() => $"{section.course.displayName} [{books.Length} Books Selected]";

    public static implicit operator TeacherBookOrder(TeacherBookOrderDetail detail) => new(detail.section.id,
        detail.section.schoolYearId, detail.section.createdTimeStamp, detail.books);
}

public readonly record struct CreateOptionGroup(ImmutableArray<string> isbns, string optionId);

public readonly record struct TeacherBookOrderGroup(string id, string option, ImmutableArray<TeacherBookRequest> isbns);

public readonly record struct CreateTeacherBookOrder(
    ImmutableArray<string> isbns,
    int quantity,
    bool fall,
    bool spring,
    string optionId = "");



public readonly record struct CreateTeacherBookRequest(string isbn, int quantity, bool fall, bool spring);
public readonly record struct CreateTeacherBookOrderGroup(ImmutableArray<CreateTeacherBookRequest> isbns, string optionId = "");

public readonly record struct SummerSectionRecord(string id, AdvisorRecord teacher, CourseRecord course, string schoolYear, DateTime submitted, ImmutableArray<SummerBookOrderListItem> books);

public readonly record struct SummerBookOrderListItem(ISBNDetail isbn, int quantity);