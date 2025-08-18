using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Bookstore.Models;

public record OrderStatus(string id, string label, string description);

public record OrderOption(string id, string label, string description)
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
public record ProtoSection(
    string id,
    CourseRecord course,
    string schoolYearId,
    bool fallOrFullYear,
    bool springOnly,
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
public record TeacherBookRequest(
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
public record TeacherBookRequestGroup(
    string groupId,
    string option,
    List<TeacherBookRequest> requestedISBNs);

/// <summary>
/// All the book orders for a particular section
/// </summary>
/// <param name="protoSectionId">ID of the section this is connected to.</param>
/// <param name="books">Groups of Book orders.</param>
public record TeacherBookOrder(
    string protoSectionId,
    string schoolYearId,
    DateTime created,
    List<TeacherBookRequest> books)
{
    public int Quantity => books.Count != 0 ? books.First().quantity : 0;
    public bool Fall => books.Any(req => req.fall);
    public bool Spring => books.Any(req => req.spring);
}

public record TeacherBookOrderDetail(
    ProtoSection section,
    List<TeacherBookRequest> books)
{
    public bool Fall => books.Any(req => req.fall);
    public bool Spring => books.Any(req => req.spring);

    public override string ToString() => $"{section.course.displayName} [{books.Count} Books Selected]";

    public static implicit operator TeacherBookOrder(TeacherBookOrderDetail detail) => new(detail.section.id,
        detail.section.schoolYearId, detail.section.createdTimeStamp, detail.books);
}

public record CreateOptionGroup(List<string> isbns, string optionId);

public record TeacherBookOrderGroup(string id, string option, List<TeacherBookRequest> isbns)
{
    public static TeacherBookOrderGroup Empty => new("", "", []);
}

public record CreateTeacherBookOrder(
    List<string> isbns,
    int quantity,
    bool fall,
    bool spring,
    string optionId = "");



public record CreateTeacherBookRequest(string isbn, int quantity, bool fall, bool spring);
public record CreateTeacherBookOrderGroup(List<CreateTeacherBookRequest> isbns, string optionId = "");

