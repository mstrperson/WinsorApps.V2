using System.Collections.Concurrent;
using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class IsbnViewModel : 
    ObservableObject,
    IDefaultValueViewModel<IsbnViewModel>,
    ICachedViewModel<IsbnViewModel, ISBNInfo, BookService>,
    ISelectable<IsbnViewModel>
{
    public event EventHandler<OdinDataViewModel>? OdinUpdateRequested;
    public event EventHandler<IsbnViewModel>? IsbnUpdateRequested;
    public event EventHandler<IsbnViewModel>? Selected;

    [ObservableProperty] bool editable = true;
     
    [ObservableProperty] string isbn = "";

    [ObservableProperty] BookBindingViewModel binding = new("Hardcover");

    [ObservableProperty] bool available = true;
    [ObservableProperty] bool isSelected;

    public string AvailableString => Available ? "Available" : "Not Available";

    public static ConcurrentBag<IsbnViewModel> ViewModelCache { get; private set; } = [];

    public static IsbnViewModel Default => throw new NotImplementedException();


    [ObservableProperty] string displayName = "";

    [ObservableProperty] bool hasOdinData = false;

    [ObservableProperty] OdinDataViewModel currentOdinData = OdinDataViewModel.Default;

    [ObservableProperty] ImmutableArray<string> bindingOptions = [];

    [ObservableProperty] string bookId = "";

    [ObservableProperty] BookInfo book;

    public IsbnViewModel(ISBNInfo info)
    {
        isbn = info.isbn;
        binding = new(info.binding);
        available = info.available;
        hasOdinData = info.hasOdinData;
        editable = false;
        displayName = $"{Isbn} [{Binding}]";
        currentOdinData = new() {Isbn = Isbn};
        CurrentOdinData.UpdateRequested += (sender, e) =>
            OdinUpdateRequested?.Invoke(this, (OdinDataViewModel) sender!);
        if (hasOdinData)
            FetchOdinData().SafeFireAndForget();
        BookService? bookService = ServiceHelper.GetService<BookService>();
        bindingOptions = bookService?.BookBindings.Select(b => $"{b}").ToImmutableArray() ?? [];
    }

    public IsbnViewModel()
    {
        editable = true;
        isbn = "";
        binding = new("Hardcover");
        available = true;
        hasOdinData = false;
        displayName = "";
        currentOdinData = new();
        CurrentOdinData.UpdateRequested += (sender, e) =>
            OdinUpdateRequested?.Invoke(this, (OdinDataViewModel) sender!);
        BookService? bookService = ServiceHelper.GetService<BookService>();
        bindingOptions = bookService?.BookBindings.Select(b => $"{b}").ToImmutableArray() ?? [];
    }

    public static implicit operator IsbnViewModel(ISBNInfo info) => new(info);

    public static implicit operator ISBNInfo(IsbnViewModel model) =>
        new(model.Isbn, $"{model.Binding}", model.Available, model.HasOdinData);

    [RelayCommand]
    public void ToggleEditable()
    {
        Editable = !Editable;
    }

    [RelayCommand]
    public void ToggleAvailable()
    {
        Available = !Available;
    }

    [RelayCommand]
    public async Task Save()
    {
        BookService? bookService = ServiceHelper.GetService<BookService>();
        LocalLoggingService? logging = ServiceHelper.GetService<LocalLoggingService>();

        if (bookService is null)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Warning,
                "Book Service was not available to fetch Odin Data from...");
            return;
        }

        await bookService.ReplaceISBN(BookId, Isbn,
            new(Isbn, Binding.Id,
                string.IsNullOrEmpty(CurrentOdinData.Plu) ? new(CurrentOdinData.Plu, CurrentOdinData.Cost) : null),
            err => logging?.LogMessage(LocalLoggingService.LogLevel.Error, err.error));
        IsbnUpdateRequested?.Invoke(this, this);
    }

    [RelayCommand]
    public async Task Delete()
    {
        BookService? bookService = ServiceHelper.GetService<BookService>();
        LocalLoggingService? logging = ServiceHelper.GetService<LocalLoggingService>();

        if (bookService is null)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Warning,
                "Book Service was not available to fetch Odin Data from...");
            return;
        }

        await bookService.DeleteIsbn(BookId, Isbn,
            err => logging?.LogMessage(LocalLoggingService.LogLevel.Error, err.error));
    }

    [RelayCommand]
    public async Task FetchOdinData()
    {
        BookService? bookService = ServiceHelper.GetService<BookService>();
        LocalLoggingService? logging = ServiceHelper.GetService<LocalLoggingService>();

        if (bookService is null)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Warning,
                "Book Service was not available to fetch Odin Data from...");
            return;
        }

        var details = await bookService.GetISBNDetails(Isbn,
            err => logging?.LogMessage(LocalLoggingService.LogLevel.Error, err.error));

        if (!details.HasValue)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Debug, $"Unable to retrive book details for ISBN: {Isbn}");
            return;
        }

        var data = details.Value;
        BookId = data.bookInfo.id;
        CurrentOdinData = data.odinData.HasValue
            ? new(data.odinData.Value) {BookId = BookId, Isbn = Isbn}
            : new() {BookId = BookId, Isbn = Isbn};
        CurrentOdinData.UpdateRequested +=
            (sender, e) => OdinUpdateRequested?.Invoke(this, (OdinDataViewModel) sender!);
    }

    public static List<IsbnViewModel> GetClonedViewModels(IEnumerable<ISBNInfo> models)
    {
        List<IsbnViewModel> result = [];
        foreach (var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(BookService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);
        _ = GetClonedViewModels(service.BooksCache.SelectMany(book => book.isbns));
    }

    public static IsbnViewModel Get(string isbn)
    {
        var vm = ViewModelCache.FirstOrDefault(i => i.Isbn == isbn);
        if (vm is null)
        {
            return new() { Isbn = isbn };
        }

        return vm;
    }

    public static IsbnViewModel Get(ISBNDetail model)
    {
        var vm = ViewModelCache.FirstOrDefault(isbn => isbn.Isbn == model.isbn);
        if (vm is null)
        {
            vm = new(new ISBNInfo(model.isbn, model.binding, true, model.odinData.HasValue));
            ViewModelCache.Add(vm);
        }
        var output = vm.Clone();
        output.Book = model.bookInfo;
        return output;
    }

    public static IsbnViewModel Get(ISBNInfo model)
    {
        var vm = ViewModelCache.FirstOrDefault(isbn => isbn.Isbn == model.isbn);
        if(vm is null)
        {
            vm = new(model);
            ViewModelCache.Add(vm);
        }
        return vm.Clone();
    }

    public IsbnViewModel Clone() => (IsbnViewModel)MemberwiseClone();

    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}
public partial class OdinDataViewModel : ObservableObject, IDefaultValueViewModel<OdinDataViewModel>
{
    private OdinData? data;

    public string BookId { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;

    public event EventHandler<OdinData>? UpdateRequested;

    [ObservableProperty]
    string plu;

    [ObservableProperty]
    double cost;

    public string CostString
    {
        get => $"{Cost:C}";
        set => Cost = value.ConvertToCurrency();
    }

    public static OdinDataViewModel Default => new();

    [ObservableProperty]
    bool isCurrent;

    public OdinDataViewModel()
    {
        plu = "";
        cost = 0;
        isCurrent = false;
    }
    public OdinDataViewModel(OdinData data)
    {
        this.data = data;
        plu = data.plu;
        cost = data.price;
        isCurrent = data.current;
    }

    [RelayCommand]
    public void SaveChange()
    {
        if(data.HasValue)
            UpdateRequested?.Invoke(this, data.Value);
    }
}