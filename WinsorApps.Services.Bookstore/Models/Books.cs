using System.Collections.Immutable;

namespace WinsorApps.Services.Bookstore.Models;

public record BookSearchFilter(string? title = null, string? isbn = null, string? publisher = null)
{
    public bool IsMatchFor(BookDetail book)
    {
        if (!book.title.Contains(title ?? "", StringComparison.InvariantCultureIgnoreCase))
            return false;

        var thisIsbn = isbn;
        if (!string.IsNullOrEmpty(thisIsbn) &&
            book.isbns.All(entry => !entry.isbn.Equals(thisIsbn, StringComparison.InvariantCultureIgnoreCase)))
            return false;

        if (!book.publisher.Contains(publisher ?? "", StringComparison.InvariantCultureIgnoreCase))
            return false;

        return true;
    }

    public bool IsMatchFor(ISBNDetail isbnInfo)
    {
        if (!isbnInfo.bookInfo.title.Contains(title ?? "", StringComparison.InvariantCultureIgnoreCase))
            return false;

        var thisIsbn = isbn;
        if (!string.IsNullOrEmpty(thisIsbn) && !isbnInfo.isbn.Equals(thisIsbn, StringComparison.InvariantCultureIgnoreCase))
            return false;

        if (!isbnInfo.bookInfo.publisher.Contains(publisher ?? "", StringComparison.InvariantCultureIgnoreCase))
            return false;

        return true;
    }

    public override string ToString()
    {
        var output = "";
        char sep = '?';
        if (!string.IsNullOrEmpty(title))
        {
            output += $"{sep}title={title}";
            sep = '&';
        }

        if (!string.IsNullOrEmpty(isbn))
        {
            output += $"{sep}isbn={isbn}";
            sep = '&';
        }

        if (!string.IsNullOrEmpty(publisher))
        {
            output += $"{sep}publisher={publisher}";
        }

        return output;
    }
}

public record OdinData(string plu, double price, bool current)
{
    public static readonly OdinData None = new("", 0, false);
}


/// <summary>
/// TODO:  why does this need to be a reference type again?  I know I did this on purpose, but why...
/// </summary>
/// <param name="id"></param>
/// <param name="binding"></param>
public record BookBinding(string id, string binding)
{
    public virtual bool Equals(BookBinding? other)
    {
        return other is not null && other.id == this.id;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }

    public override string ToString() => binding;
}

public record BookDetail(
    string id,
    string title,
    List<string> authors,
    string edition,
    DateOnly publicationDate,
    string publisher,
    List<ISBNInfo> isbns)
{
    public static readonly BookDetail Empty = new("", "", [], "", default, "", []);
    public override string ToString() => $"{title}, {authors.Aggregate((a, b) => $"{a}; {b}")} [{publisher}]";

    public static implicit operator BookInfo(BookDetail details) =>
        new(details.id, details.title, details.authors, details.edition, details.publicationDate, details.publisher);
}

public record BookInfo(
    string id,
    string title,
    List<string> authors,
    string edition,
    DateOnly publicationDate,
    string publisher);

public record ISBNInfo(string isbn, string binding, bool available, bool hasOdinData)
{
    public static readonly ISBNInfo Empty = new("", "None", false, false);
}

public record ISBNDetail(string isbn, string binding, OdinData? odinData, BookInfo bookInfo);

public record CreateBook(
    string title,
    string publisher,
    List<string> authors,
    DateOnly publishDate = default,
    string edition = "");

public record CreateISBN(string isbn, string bindingId, CreateOdinData? odinData = null);

public record CreateOdinData(string plu, double price);