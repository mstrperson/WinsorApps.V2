using System.Collections.Immutable;

namespace WinsorApps.Services.Bookstore.Models;

public readonly record struct BookSearchFilter(string? title = null, string? isbn = null, string? publisher = null)
{
    public bool IsMatchFor(BookDetail book)
    {
        if (!book.title.ToLowerInvariant().Contains((title ?? "").ToLowerInvariant()))
            return false;

        var thisIsbn = isbn;
        if (!string.IsNullOrEmpty(thisIsbn) &&
            book.isbns.All(entry => entry.isbn.ToUpperInvariant() != thisIsbn.ToUpperInvariant()))
            return false;

        if (!book.publisher.ToLowerInvariant().Contains((publisher ?? "").ToLowerInvariant()))
            return false;

        return true;
    }

    public bool IsMatchFor(ISBNDetail isbnInfo)
    {
        if (!isbnInfo.bookInfo.title.ToLowerInvariant().Contains((title ?? "").ToLowerInvariant()))
            return false;

        var thisIsbn = isbn;
        if (!string.IsNullOrEmpty(thisIsbn) && isbnInfo.isbn.ToUpperInvariant() != thisIsbn.ToUpperInvariant())
            return false;

        if (!isbnInfo.bookInfo.publisher.ToLowerInvariant().Contains((publisher ?? "").ToLowerInvariant()))
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

public readonly record struct OdinData(string plu, double price, bool current);

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

public readonly record struct BookDetail(
    string id,
    string title,
    ImmutableArray<string> authors,
    string edition,
    DateOnly publicationDate,
    string publisher,
    ImmutableArray<ISBNInfo> isbns)
{
    public override string ToString() => $"{title}, {authors.Aggregate((a, b) => $"{a}; {b}")} [{publisher}]";

    public static implicit operator BookInfo(BookDetail details) =>
        new(details.id, details.title, details.authors, details.edition, details.publicationDate, details.publisher);
}

public readonly record struct BookInfo(
    string id,
    string title,
    ImmutableArray<string> authors,
    string edition,
    DateOnly publicationDate,
    string publisher);

public readonly record struct ISBNInfo(string isbn, string binding, bool available, bool hasOdinData);

public readonly record struct ISBNDetail(string isbn, string binding, OdinData? odinData, BookInfo bookInfo);

public readonly record struct CreateBook(
    string title,
    string publisher,
    ImmutableArray<string> authors,
    DateOnly publishDate = default,
    string edition = "");

public readonly record struct CreateISBN(string isbn, string bindingId, CreateOdinData? odinData = null);

public readonly record struct CreateOdinData(string plu, double price);