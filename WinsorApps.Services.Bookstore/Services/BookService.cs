using System.Collections.Immutable;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Bookstore.Services;

public class BookService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public BookService(LocalLoggingService logging, ApiService api)
        {
            _logging = logging;
            _api = api;
        }

        public async Task Initialize(ErrorAction onError)
        {
            if(Ready) { return; }

            while(!_api.Ready)
            {
                await Task.Delay(500);
            }

            var bindingTask = _api.SendAsync<ImmutableArray<BookBinding>>(HttpMethod.Get,
                "api/books/list-bindings", onError: onError);
            bindingTask.WhenCompleted(() =>
            {
                _bookBindings = bindingTask.Result;
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Loaded {_bookBindings.Value.Length} book Bindings");
            });

            var bookCache = _api.SendAsync<List<BookDetail>>(HttpMethod.Get,
                "api/books", onError: onError);

            bookCache.WhenCompleted(() =>
            {
                _bookCache = bookCache.Result;
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Loaded {_bookCache?.Count ?? 0} Books");
            });

            await Task.WhenAll(bookCache, bindingTask);

            Ready = true;
        }

        public bool Ready { get; private set; } = false;

        private List<BookDetail>? _bookCache;

        public ImmutableArray<BookDetail> BooksCache
        {
            get
            {
                if (_bookCache is null)
                    throw new ServiceNotReadyException(_logging, $"Book service has not yet been fully initialized");

                return _bookCache.ToImmutableArray();
            }
        }

        private ImmutableArray<BookBinding>? _bookBindings;
        public ImmutableArray<BookBinding> BookBindings
        {
            get
            {
                if (!_bookBindings.HasValue)
                    throw new ServiceNotReadyException(_logging, $"Book service has not yet been fully initialized");

                return _bookBindings.Value;
            }
        }

        public async Task RefreshBookCache(ErrorAction onError)
        {
            var cache = await _api.SendAsync<List<BookDetail>>(HttpMethod.Get,
                "api/books", onError: onError);

            if(cache is not null)
                _bookCache = cache;
        }

        public async Task<ISBNDetail?> GetISBNDetails(string isbn, ErrorAction onError) =>
            await _api.SendAsync<ISBNDetail?>(HttpMethod.Get, $"api/books/isbn/{isbn}", onError: onError);

        public async Task<ImmutableArray<ISBNDetail>> SearchISBN(BookSearchFilter filter, ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<ISBNDetail>>(HttpMethod.Get,
                $"api/books/isbn/search{filter}", onError: onError);

        public async Task<ImmutableArray<BookDetail>> SearchBooks(BookSearchFilter filter, ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<BookDetail>>(HttpMethod.Get,
                $"api/books/search{filter}", onError: onError);

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

        private BookDetail? HandleBookResult(BookDetail result)
        {
            if (result == default)
                return null;

            if (_bookCache!.TrueForAll(book => book.id != result.id))
            {
                _bookCache!.Add(result);
            }
            else
            {
                var old = _bookCache.First(book => book.id == result.id);
                _bookCache.Replace(old, result);
            }

            return result;
        }

        public async Task<BookDetail?> ReplaceISBN(string bookId, string oldIsbn, CreateISBN newIsbn, ErrorAction onError)
        {
            var isbnInfo = await _api.SendAsync<CreateISBN, ISBNInfo?>(HttpMethod.Put,
                $"api/books/{bookId}/{oldIsbn}/replace", newIsbn, onError: onError);
            if (!isbnInfo.HasValue) return null;

            var book = await _api.SendAsync<BookDetail>(HttpMethod.Get, $"api/books/{bookId}", onError: onError);

            return HandleBookResult(book);
        }

        public async Task<BookDetail?> UpdateOdinData(string bookId, string isbn, CreateOdinData odinData, ErrorAction onError)
        {
            var isbnInfo = await _api.SendAsync<CreateOdinData, ISBNDetail?>(HttpMethod.Post,
                $"api/books/isbn/{isbn}/price-data", odinData, onError: onError);

            if (!isbnInfo.HasValue) return null;

            var book = await _api.SendAsync<BookDetail>(HttpMethod.Get, $"api/books/{bookId}", onError: onError);

            return HandleBookResult(book);
        }

        public async Task<BookDetail?> DeleteIsbn(string bookId, string isbn, ErrorAction onError)
        {
            var result = await _api.SendAsync<ISBNInfo?>(HttpMethod.Delete, $"api/books/isbn/{isbn}",onError: onError);

            if (!result.HasValue)
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
    }