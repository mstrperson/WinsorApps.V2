using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class BookInfoViewModel :
    ObservableObject
{
    [ObservableProperty] private string title = "";
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string authorList = "";
    [ObservableProperty] private string edition = "";
    [ObservableProperty] private DateTime publicationDate;
    [ObservableProperty] private string publisher = "";

    public BookInfoViewModel() { }
    public BookInfoViewModel(BookInfo bookInfo)
    {
        Title = bookInfo.title;
        Id = bookInfo.id;
        AuthorList = bookInfo.authors.DelimeteredList();
        Edition = bookInfo.edition;
        PublicationDate = bookInfo.publicationDate.ToDateTime(default);
        Publisher = bookInfo.publisher;
    }
}

public partial class BookViewModel :
    ObservableObject, 
    IErrorHandling,
    IDefaultValueViewModel<BookViewModel>,
    IModelCarrier<BookViewModel, BookDetail>,
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
    [ObservableProperty] private ObservableCollection<IsbnViewModel> isbns = [];

    public Optional<BookDetail> Model { get; private set; } = Optional<BookDetail>.None();

    public static List<BookViewModel> ViewModelCache { get; private set; } = [];

    public static BookViewModel Empty => new();

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
        isbns = book.isbns == default ? [] : [.. book.isbns.Select(entry => (IsbnViewModel)entry)];
        foreach (var entry in Isbns)
        {
            entry.IsbnUpdateRequested += (sender, e) => SaveIsbnRequested?.Invoke(this, e);
            entry.OdinUpdateRequested += (sender, e) => SaveOdinDataRequested?.Invoke(this, e);
        }
        Model = Optional<BookDetail>.Some(book);
    }

    public static implicit operator BookViewModel(BookDetail book) => new(book);
    public static implicit operator BookDetail(BookViewModel vm) => vm.Model.Reduce(BookDetail.Empty);

    public override string ToString() => $"{Model}";

    public CreateBook GetUpdateDetails() => new(Title, Publisher, [.. AuthorList.DelimeteredList()], DateOnly.FromDateTime(PublicationDate), Edition);

    [RelayCommand]
    public async Task Save()
    {
        var bookService = ServiceHelper.GetService<BookService>();
        var logging = ServiceHelper.GetService<LocalLoggingService>();

        var data = GetUpdateDetails();

        if (IsNew)
        {
            var book = await bookService.CreateNewBook(data, err => OnError?.Invoke(this, err));
            if (book is null)
            {
                logging?.LogMessage(LocalLoggingService.LogLevel.Debug, $"Creating a new Book was unsuccessful.. {data}");
                return;
            }
            Model = Optional<BookDetail>.Some(book);

            ViewModelCache.Add(new(book));

            return;
        }

        var updatedBook = await bookService.UpdateBook(Model.Reduce(BookDetail.Empty).id, data, err => OnError?.Invoke(this, err));
        if (updatedBook is null)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Debug, $"Updating Book {Model.Reduce(BookDetail.Empty).id} was unsuccessful.. {data}");
            return;
        }

        Model = Optional<BookDetail>.Some(updatedBook);
        var old = ViewModelCache.FirstOrDefault(vm => vm.Id == updatedBook.id);
        if (old is null)
            ViewModelCache.Add(new(updatedBook));
        else
        {
            ViewModelCache = [ .. ViewModelCache.Except([old]), new(updatedBook)];
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