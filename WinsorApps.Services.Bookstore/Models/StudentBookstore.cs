using System.Collections.Immutable;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Bookstore.Models;

/// <summary>
/// All the Book Orders that a Student has submitted for a particular section.
/// </summary>
/// <param name="sectionId">section.id of a class on the student's schedule</param>
/// <param name="selectedBooks">Books that the student has indicated that they intend to purchase from the school</param>
public record StudentSectionBookOrder(string sectionId, UserRecord student, SectionMinimalRecord section, List<StudentBookRequest> selectedBooks);

/// <summary>
/// Option Groups indicate whether a Student is expected to purchase `All` or the books in this group, or 
/// if the student should select only one version of the text (e.g. The book is available in Print or Electronic format).
/// </summary>
/// <param name="option">Informs the student what their choices are.</param>
/// <param name="isbns">All the Books listed in this group</param>
public record StudentSectionBookOptionGroup(string option, List<ISBNDetail> isbns);

/// <summary>
/// List all the Books required for a particular class.
/// </summary>
/// <param name="sectionId">the section.id of a Section that can be found in the RegistrarService.MyAcademicSchedule list.</param>
/// <param name="studentSections">Collection of book order groups.  These groups may contain 1 or more books.</param>
public record StudentSectionBookRequirements(string sectionId, List<StudentSectionBookOptionGroup> studentSections);

/// <summary>
/// Semester Book List.  This lists all the books required for a particular semester.
/// </summary>
/// <param name="term">one of `fall` or `spring`</param>
/// <param name="schedule">Theoretically, there should be 1 entry in this list for each of a student's academic classes in the selected semester.</param>
public record StudentSemesterBookList(string term, List<StudentSectionBookRequirements> schedule);

/// <summary>
/// This is a single record that indicates that a Student intends to purchase a specific ISBN from the school.
/// </summary>
/// <param name="submitted">Timestamp for when the student submitted the order.</param>
/// <param name="status">current status of the order in the ordering process.</param>
/// <param name="isbn">specific ISBN that has been indicated.</param>
public record StudentBookRequest(DateTime submitted, string status, string isbn);

public record StudentOrderStatus
{
    private readonly string _status;

    public static readonly StudentOrderStatus Submitted = new("Submitted");
    public static readonly StudentOrderStatus Recieved = new("Recieved");
    public static readonly StudentOrderStatus Withdrawn = new("Withdrawn");
    public static readonly StudentOrderStatus Completed = new("Completed");

    public static implicit operator StudentOrderStatus(string str) => str.ToLowerInvariant().Trim() switch
    {
        "submitted" => Submitted,
        "recieved" => Recieved,
        "withdrawn" => Withdrawn,
        "completed" => Completed,
        _ => throw new InvalidCastException($"{str} is not a valid status")
    };

    public static implicit operator string(StudentOrderStatus status) => status._status;

    public override string ToString() => $"{_status}";

    private StudentOrderStatus(string status) => _status = status;
}