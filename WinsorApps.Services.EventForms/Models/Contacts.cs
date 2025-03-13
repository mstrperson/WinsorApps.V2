using System.Text.RegularExpressions;

namespace WinsorApps.Services.EventForms.Models;

public record Contact(string id, string firstName, string lastName,
        string email, string phone, string? associatedUserId, string ownerId, bool isPublic)
{
    private static readonly Regex _studentEmailPattern = new(@"[^.]+\.[^@]+@winsor\.edu", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool isStudent => _studentEmailPattern.IsMatch(email);

    public string FullName => $"{firstName} {lastName}";

    public static Contact Empty => new("", "", "", "", "", null, "", false);
}

public record NewContact(string firstName, string lastName,
    string email = "", string phone = "", bool isPublic = false);

