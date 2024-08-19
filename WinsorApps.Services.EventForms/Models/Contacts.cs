using System.Text.RegularExpressions;

namespace WinsorApps.Services.EventForms.Models;

public readonly record struct Contact(string id, string firstName, string lastName,
        string email, string phone, string? associatedUserId, string ownerId, bool isPublic)
{
    private static Regex _studentEmailPattern = new(@"[^.]+\.[^@]+@winsor\.edu", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool isStudent => _studentEmailPattern.IsMatch(email);

    public string FullName => $"{firstName} {lastName}";

    public static Contact Empty => new Contact("", "", "", "", "", null, "", false);
}

public readonly record struct NewContact(string firstName, string lastName,
    string email = "", string phone = "", bool isPublic = false);

