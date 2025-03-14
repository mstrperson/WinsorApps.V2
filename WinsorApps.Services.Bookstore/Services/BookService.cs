using System.Collections.Immutable;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Bookstore.Services;

public class BookService(LocalLoggingService logging, ApiService api) : IAsyncInitService
{
    private readonly ApiService _api = api;
    private readonly LocalLoggingService _logging = logging;

    public async Task Initialize(ErrorAction onError)
    {
        if (Ready) { return; }

        await _api.WaitForInit(onError);
        
        Started = true;

        var orderOptionTask = _api.SendAsync<List<OrderOption>>(HttpMethod.Get,
            "api/book-orders/order-options", onError: onError);
        orderOptionTask.WhenCompleted(() =>
        {
            if (orderOptionTask.IsCompletedSuccessfully)
            {
                OrderOptions = orderOptionTask.Result ?? [];
                Progress += 0.33;
            }
        });

        var bindingTask = _api.SendAsync<List<BookBinding>>(HttpMethod.Get,
            "api/books/list-bindings", onError: onError);
        bindingTask.WhenCompleted(() =>
        {
            BookBindings = bindingTask.Result ?? [];
            Progress += 0.33;
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Loaded {BookBindings.Count} book Bindings");
        });

        var bookCache = _api.SendAsync<List<BookDetail>>(HttpMethod.Get,
            "api/books", onError: onError);

        bookCache.WhenCompleted(() =>
        {
            BooksCache = bookCache.Result ?? [];
            Progress += 0.33;
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Loaded {BooksCache.Count} Books");
        });


        
        await Task.WhenAll(bookCache, bindingTask);
        Progress = 1;
        Ready = true;
    }

    public bool Ready { get; private set; } = false;

    public List<OrderOption> OrderOptions { get; private set; } = [];

    public List<BookDetail> BooksCache { get; private set; } = [];

    public List<BookBinding> BookBindings { get; private set; } = [];

    public bool Started { get; private set; }

    public double Progress { get; private set; }

    public string CacheFileName => throw new NotImplementedException();

    public async Task Refresh(ErrorAction onError)
    {
        var cache = await _api.SendAsync<List<BookDetail>>(HttpMethod.Get,
            "api/books", onError: onError);

        if (cache is not null)
            BooksCache = cache;
    }

    public async Task<ISBNDetail?> GetISBNDetails(string isbn, ErrorAction onError) =>
        await _api.SendAsync<ISBNDetail?>(HttpMethod.Get, $"api/books/isbn/{isbn}", onError: onError);

    public async Task<List<ISBNDetail>> SearchISBN(BookSearchFilter filter, ErrorAction onError) =>
        await _api.SendAsync<List<ISBNDetail>>(HttpMethod.Get,
            $"api/books/isbn/search{filter}", onError: onError) ?? [];

    public async Task<List<BookDetail>> SearchBooks(BookSearchFilter filter, ErrorAction onError) =>
        BooksCache.Any(filter.IsMatchFor) ?
        [.. BooksCache.Where(filter.IsMatchFor)] :
        await _api.SendAsync<List<BookDetail>>(HttpMethod.Get,
            $"api/books/search{filter}", onError: onError) ?? [];

    public async Task<BookDetail?> CreateNewBook(CreateBook newBook, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateBook, BookDetail>(HttpMethod.Post,
            "api/books", newBook, onError: onError);
        return HandleBookResult(result);
    }

    public async Task<BookDetail?> UpdateBook(string bookId, CreateBook updatedDetails, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateBook, BookDetail>(HttpMethod.Put,
            $"api/books/{bookId}", updatedDetails, onError: onError);

        return HandleBookResult(result);
    }

    private BookDetail? HandleBookResult(BookDetail? result)
    {
        if (result is null)
            return null;

        if (BooksCache.TrueForAll(book => book.id != result.id))
        {
            BooksCache.Add(result);
        }
        else
        {
            var old = BooksCache.First(book => book.id == result.id);
            BooksCache.Replace(old, result);
        }

        return result;
    }

    public async Task<BookDetail?> ReplaceISBN(string bookId, string oldIsbn, CreateISBN newIsbn, ErrorAction onError)
    {
        var isbnInfo = await _api.SendAsync<CreateISBN, ISBNInfo?>(HttpMethod.Put,
            $"api/books/{bookId}/{oldIsbn}/replace", newIsbn, onError: onError);
        if (isbnInfo is null) return null;

        var book = await _api.SendAsync<BookDetail>(HttpMethod.Get, $"api/books/{bookId}", onError: onError);

        return HandleBookResult(book);
    }

    public async Task<BookDetail?> UpdateOdinData(string bookId, string isbn, CreateOdinData odinData, ErrorAction onError)
    {
        var isbnInfo = await _api.SendAsync<CreateOdinData, ISBNDetail?>(HttpMethod.Post,
            $"api/books/isbn/{isbn}/price-data", odinData, onError: onError);

        if (isbnInfo is null) return null;

        var book = await _api.SendAsync<BookDetail>(HttpMethod.Get, $"api/books/{bookId}", onError: onError);

        return HandleBookResult(book);
    }

    public async Task<BookDetail?> DeleteIsbn(string bookId, string isbn, ErrorAction onError)
    {
        var result = await _api.SendAsync<ISBNInfo?>(HttpMethod.Delete, $"api/books/isbn/{isbn}", onError: onError);

        if (result is null)
            return null;

        var book = await _api.SendAsync<BookDetail>(HttpMethod.Get, $"api/books/{bookId}", onError: onError);


        return HandleBookResult(book);
    }

    public async Task<BookDetail?> AddIsbnToBook(string bookId, CreateISBN newIsbn, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateISBN, BookDetail>(HttpMethod.Post,
            $"api/books/{bookId}/isbn", newIsbn, onError: onError);

        return HandleBookResult(result);
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }

    public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }
    public async Task SaveCache()
    {
        throw new NotImplementedException();
    }

    public bool LoadCache()
    {
        throw new NotImplementedException();
    }
}