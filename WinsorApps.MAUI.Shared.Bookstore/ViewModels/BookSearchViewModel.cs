using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class BookSearchViewModel : 
    ObservableObject, 
    ICachedSearchViewModel<BookViewModel>
{
    public event EventHandler<BookViewModel>? BookSelected;
    public event EventHandler<ObservableCollection<BookViewModel>>? OnMultipleResult;
    public event EventHandler<BookViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults;

    [ObservableProperty] private bool searchByAuthor;
    [ObservableProperty] private string authorSearch = "";
    [ObservableProperty] private bool searchByPublisher;
    [ObservableProperty] private string publisherSearch = "";
    [ObservableProperty] private bool searchByTitle;
    [ObservableProperty] private string titleSearch = "";
    [ObservableProperty] private bool searchByIsbn;
    [ObservableProperty] private string isbnSearch = "";

    [ObservableProperty] private ObservableCollection<BookViewModel> options = [];

    private readonly BookService _bookService;
    private readonly LocalLoggingService _logging;

    [ObservableProperty]
    private ObservableCollection<BookViewModel> available = [];
    [ObservableProperty]
    private ObservableCollection<BookViewModel> allSelected = [];
    [ObservableProperty]
    private BookViewModel selected = BookViewModel.Empty;
    [ObservableProperty]
    private SelectionMode selectionMode = SelectionMode.Multiple;
    [ObservableProperty]
    private string searchText = "";
    [ObservableProperty]
    private bool isSelected;
    [ObservableProperty]
    private bool showOptions;

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
            books = [.. books
                .Where(book =>
                    book.authors.Any(auth => auth.Contains(AuthorSearch, StringComparison.InvariantCultureIgnoreCase)))];

        if (SearchByIsbn)
            books = [.. books
                .Where(book =>
                    book.isbns.Any(isbn => isbn.isbn.Contains(IsbnSearch)))];

        if (SearchByPublisher)
            books = [.. books
                .Where(book =>
                    book.publisher.Contains(PublisherSearch, StringComparison.InvariantCultureIgnoreCase))];

        if (SearchByTitle)
            books = [.. books
                .Where(book =>
                    book.title.Contains(TitleSearch, StringComparison.InvariantCultureIgnoreCase))];

        Options = [..BookViewModel.GetClonedViewModels(books)];
        foreach (var vm in Options)
            vm.Selected += (_, book) => BookSelected?.Invoke(this, book);
    }

    [RelayCommand]
    public void Clear()
    {
        SearchByAuthor = false;
        AuthorSearch = "";
        SearchByIsbn = false;
        IsbnSearch = "";
        SearchByPublisher = false;
        PublisherSearch = "";
        SearchByTitle = false;
        TitleSearch = "";
        Options = [];
        Selected = BookViewModel.Empty;
        IsSelected = false;
        SearchText = "";
    }

    public void Select(BookViewModel item)
    {
        BookSelected?.Invoke(this, item);
    }

    async Task IAsyncSearchViewModel<BookViewModel>.Search() => await Task.Run(Search);
}

public partial class BookISBNSelectionViewModel :
    ObservableObject,
    IBusyViewModel,
    IErrorHandling

{
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";
    [ObservableProperty] private BookSearchViewModel search = new();
    [ObservableProperty] private bool bookSelected;
    [ObservableProperty] private BookViewModel selectedBook = BookViewModel.Empty;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<List<IsbnViewModel>>? IsbnsSelected;

    public BookISBNSelectionViewModel()
    {
        Search.OnSingleResult += (_, book) =>
        {
            BookSelected = true;
            SelectedBook = book;
        };
    }

    [RelayCommand]
    public void Submit() => IsbnsSelected?.Invoke(this, [.. SelectedBook.Isbns.Where(vm => vm.IsSelected)]);

    [RelayCommand]
    public void Clear()
    {
        SelectedBook = BookViewModel.Empty;
        BookSelected = false;
        Search.Clear();
    }
}