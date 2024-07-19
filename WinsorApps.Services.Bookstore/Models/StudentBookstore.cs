using System.Collections.Immutable;

namespace WinsorApps.Services.Bookstore.Models;

/// <summary>
/// All the Book Orders that a Student has submitted for a particular section.
/// </summary>
/// <param name="sectionId">section.id of a class on the student's schedule</param>
/// <param name="selectedBooks">Books that the student has indicated that they intend to purchase from the school</param>
public readonly record struct StudentSectionBookOrder(string sectionId, ImmutableArray<StudentBookRequest> selectedBooks);

/// <summary>
/// Option Groups indicate whether a Student is expected to purchase `All` or the books in this group, or 
/// if the student should select only one version of the text (e.g. The book is available in Print or Electronic format).
/// </summary>
/// <param name="option">Informs the student what their choices are.</param>
/// <param name="isbns">All the Books listed in this group</param>
public readonly record struct StudentSectionBookOptionGroup(string option, ImmutableArray<ISBNDetail> isbns);

/// <summary>
/// List all the Books required for a particular class.
/// </summary>
/// <param name="sectionId">the section.id of a Section that can be found in the RegistrarService.MyAcademicSchedule list.</param>
/// <param name="studentSections">Collection of book order groups.  These groups may contain 1 or more books.</param>
public readonly record struct StudentSectionBookRequirements(string sectionId, ImmutableArray<StudentSectionBookOptionGroup> studentSections);

/// <summary>
/// Semester Book List.  This lists all the books required for a particular semester.
/// </summary>
/// <param name="term">one of `fall` or `spring`</param>
/// <param name="schedule">Theoretically, there should be 1 entry in this list for each of a student's academic classes in the selected semester.</param>
public readonly record struct StudentSemesterBookList(string term, ImmutableArray<StudentSectionBookRequirements> schedule);

/// <summary>
/// This is a single record that indicates that a Student intends to purchase a specific ISBN from the school.
/// </summary>
/// <param name="submitted">Timestamp for when the student submitted the order.</param>
/// <param name="status">current status of the order in the ordering process.</param>
/// <param name="isbn">specific ISBN that has been indicated.</param>
public readonly record struct StudentBookRequest(DateTime submitted, string status, string isbn);