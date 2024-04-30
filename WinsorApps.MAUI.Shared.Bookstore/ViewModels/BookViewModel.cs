using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class BookViewModel : ObservableObject
{
    public event EventHandler<BookViewModel>? Selected;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<OdinDataViewModel>? SaveOdinDataRequested;
    public event EventHandler<IsbnViewModel>? SaveIsbnRequested;
    public event EventHandler<BookViewModel>? SaveBookRequested;
    public event EventHandler<BookViewModel>? EditRequested;
    
    [ObservableProperty] private string title;
    [ObservableProperty] private string id;
    [ObservableProperty] private string authorList;
    [ObservableProperty] private string edition;
    [ObservableProperty] private DateTime publicationDate;
    [ObservableProperty] private string publisher;
    [ObservableProperty] private bool isNew;
    [ObservableProperty] private List<IsbnViewModel> isbns;
    
    private BookDetail _book;
        public BookViewModel(BookDetail book)
        {
            isNew = string.IsNullOrEmpty(book.id);
            id = book.id;
            title = book.title;
            authorList = book.authors == default ? "" : book.authors.DelimeteredList();
            edition = book.edition;
            publicationDate = book.publicationDate.ToDateTime(default);
            publisher = book.publisher;
            isbns = book.isbns == default ? new() : book.isbns.Select(entry => (IsbnViewModel)entry).ToList();
            foreach(var entry in Isbns)
            {
                entry.IsbnUpdateRequested += (sender, e) => SaveIsbnRequested?.Invoke(this, e);
                entry.OdinUpdateRequested += (sender, e) => SaveOdinDataRequested?.Invoke(this, e);
            }
            _book = book;
        }

        public static implicit operator BookViewModel(BookDetail book) => new(book);
        public static implicit operator BookDetail(BookViewModel vm) => vm._book;

        public override string ToString() => $"{_book}";

        public CreateBook GetUpdateDetails() => new(Title, Publisher, [..AuthorList.DelimeteredList()], DateOnly.FromDateTime(PublicationDate), Edition);

        [RelayCommand]
        public async Task Save()
        {
            BookService? bookService = ServiceHelper.GetService<BookService>();
            LocalLoggingService? logging = ServiceHelper.GetService<LocalLoggingService>();
            if(bookService is null)
            {
                logging?.LogMessage(LocalLoggingService.LogLevel.Warning, $"Failed to retrieve BookService while saving book {_book}");
                OnError?.Invoke(this, new("Service Missing", "Unable to retrieve the Book Service for some reason..."));
                return;
            }

            var data = GetUpdateDetails();

            if(IsNew)
            {
                var book = await bookService.CreateNewBook(data, err => OnError?.Invoke(this, err));
                if(!book.HasValue)
                {
                    logging?.LogMessage(LocalLoggingService.LogLevel.Debug, $"Creating a new Book was unsuccessful.. {data}");
                    return;
                }
                _book = book.Value;
                return;
            }

            var updatedBook = await bookService.UpdateBook(_book.id, data, err => OnError?.Invoke(this, err));
            if(!updatedBook.HasValue)
            {
                logging?.LogMessage(LocalLoggingService.LogLevel.Debug, $"Updating Book {_book.id} was unsuccessful.. {data}");
                return;
            }

            _book = updatedBook.Value;
        }

        [RelayCommand]
        public void Edit() =>
            EditRequested?.Invoke(this, this);

        [RelayCommand]
        public void Select() => 
            Selected?.Invoke(this, this);
}