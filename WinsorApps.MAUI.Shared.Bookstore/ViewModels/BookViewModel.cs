using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class BookViewModel :
    ObservableObject, 
    IErrorHandling,
    IDefaultValueViewModel<BookViewModel>,
    ICachedViewModel<BookViewModel, BookDetail, BookService>
{
    public event EventHandler<BookViewModel>? Selected;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<OdinDataViewModel>? SaveOdinDataRequested;
    public event EventHandler<IsbnViewModel>? SaveIsbnRequested;
    public event EventHandler<BookViewModel>? SaveBookRequested;
    public event EventHandler<BookViewModel>? EditRequested;

    [ObservableProperty] private string title = "";
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string authorList = "";
    [ObservableProperty] private string edition = "";
    [ObservableProperty] private DateTime publicationDate;
    [ObservableProperty] private string publisher = "";
    [ObservableProperty] private bool isNew;
    [ObservableProperty] private List<IsbnViewModel> isbns = [];

    private BookDetail _book = new("", "", [], "", default, "", []);

    public static ConcurrentBag<BookViewModel> ViewModelCache { get; private set; } = [];

    public static BookViewModel Default => new();

    public BookViewModel()
    {
        // empty view model.
        isNew = true;
    }

    private BookViewModel(BookDetail book)
    {
        isNew = string.IsNullOrEmpty(book.id);
        id = book.id;
        title = book.title;
        authorList = book.authors == default ? "" : book.authors.DelimeteredList();
        edition = book.edition;
        publicationDate = book.publicationDate.ToDateTime(default);
        publisher = book.publisher;
        isbns = book.isbns == default ? new() : book.isbns.Select(entry => (IsbnViewModel)entry).ToList();
        foreach (var entry in Isbns)
        {
            entry.IsbnUpdateRequested += (sender, e) => SaveIsbnRequested?.Invoke(this, e);
            entry.OdinUpdateRequested += (sender, e) => SaveOdinDataRequested?.Invoke(this, e);
        }
        _book = book;
    }

    public static implicit operator BookViewModel(BookDetail book) => new(book);
    public static implicit operator BookDetail(BookViewModel vm) => vm._book;

    public override string ToString() => $"{_book}";

    public CreateBook GetUpdateDetails() => new(Title, Publisher, [.. AuthorList.DelimeteredList()], DateOnly.FromDateTime(PublicationDate), Edition);

    [RelayCommand]
    public async Task Save()
    {
        BookService bookService = ServiceHelper.GetService<BookService>();
        LocalLoggingService logging = ServiceHelper.GetService<LocalLoggingService>();

        var data = GetUpdateDetails();

        if (IsNew)
        {
            var book = await bookService.CreateNewBook(data, err => OnError?.Invoke(this, err));
            if (!book.HasValue)
            {
                logging?.LogMessage(LocalLoggingService.LogLevel.Debug, $"Creating a new Book was unsuccessful.. {data}");
                return;
            }
            _book = book.Value;

            ViewModelCache.Add(new(_book));

            return;
        }

        var updatedBook = await bookService.UpdateBook(_book.id, data, err => OnError?.Invoke(this, err));
        if (!updatedBook.HasValue)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Debug, $"Updating Book {_book.id} was unsuccessful.. {data}");
            return;
        }

        _book = updatedBook.Value;
        var old = ViewModelCache.FirstOrDefault(vm => vm.Id == _book.id);
        if (old is null)
            ViewModelCache.Add(new(_book));
        else
        {
            ViewModelCache = [ .. ViewModelCache.Except([old]), new(_book)];
        }
    }

    [RelayCommand]
    public void Edit() =>
        EditRequested?.Invoke(this, this);

    [RelayCommand]
    public void Select() =>
        Selected?.Invoke(this, this);

    public static List<BookViewModel> GetClonedViewModels(IEnumerable<BookDetail> models)
    {
        List<BookViewModel> result = [];
        foreach (var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(BookService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

        _ = GetClonedViewModels(service.BooksCache);
    }

    public static BookViewModel Get(BookDetail model)
    {
        var vm = ViewModelCache.FirstOrDefault(book => book.Id == model.id);
        if (vm is null)
        {
            vm = new(model);
            ViewModelCache.Add(vm);
        }
        return vm.Clone();
    }

    public BookViewModel Clone() => (BookViewModel)MemberwiseClone();
}