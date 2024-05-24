using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class BookSearchViewModel : ObservableObject
{
    public event EventHandler<BookViewModel>? BookSelected;
    
    [ObservableProperty] private bool searchByAuthor;
    [ObservableProperty] private string authorSearch = "";
    [ObservableProperty] private bool searchByPublisher;
    [ObservableProperty] private string publisherSearch = "";
    [ObservableProperty] private bool searchByTitle;
    [ObservableProperty] private string titleSearch = "";
    [ObservableProperty] private bool searchByIsbn;
    [ObservableProperty] private string isbnSearch = "";

    [ObservableProperty] private ImmutableArray<BookViewModel> searchResults = [];

    private readonly BookService _bookService;
    private readonly LocalLoggingService _logging;

    public BookSearchViewModel()
    {
        _bookService = ServiceHelper.GetService<BookService>();
        _logging = ServiceHelper.GetService<LocalLoggingService>();
    }

    [RelayCommand]
    public void Search()
    {
        var books = _bookService.BooksCache;
        if (SearchByAuthor)
            books = books
                .Where(book =>
                    book.authors.Any(auth => auth.Contains(AuthorSearch, StringComparison.InvariantCultureIgnoreCase)))
                .ToImmutableArray();

        if (SearchByIsbn)
            books = books
                .Where(book =>
                    book.isbns.Any(isbn => isbn.isbn.Contains(IsbnSearch)))
                .ToImmutableArray();

        if (SearchByPublisher)
            books = books
                .Where(book =>
                    book.publisher.Contains(PublisherSearch, StringComparison.InvariantCultureIgnoreCase))
                .ToImmutableArray();

        if (SearchByTitle)
            books = books
                .Where(book =>
                    book.title.Contains(TitleSearch, StringComparison.InvariantCultureIgnoreCase))
                .ToImmutableArray();

        SearchResults = books.Select(book => new BookViewModel(book)).ToImmutableArray();
        foreach (var vm in SearchResults)
            vm.Selected += (_, book) => BookSelected?.Invoke(this, book);
    }
}