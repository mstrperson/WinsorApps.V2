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

    [ObservableProperty] private bool editable = true;
     
    [ObservableProperty] private string isbn = "";

    [ObservableProperty] private BookBindingViewModel binding = new("Hardcover");

    [ObservableProperty] private bool available = true;
    [ObservableProperty] private bool isSelected;

    public string AvailableString => Available ? "Available" : "Not Available";

    public static List<IsbnViewModel> ViewModelCache { get; private set; } = [];

    public static IsbnViewModel Empty => new();


    [ObservableProperty] private string displayName = "";

    [ObservableProperty] private bool hasOdinData = false;

    [ObservableProperty] private OdinDataViewModel currentOdinData = OdinDataViewModel.Empty;

    [ObservableProperty] private List<string> bindingOptions = [];

    [ObservableProperty] private string bookId = "";

    [ObservableProperty] private BookInfoViewModel book = new();

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
        var bookService = ServiceHelper.GetService<BookService>();
        bindingOptions = [.. bookService.BookBindings.Select(b => $"{b}")];
        LoadBookDetails();
    }

    private IsbnViewModel(string isbn)
    {
        editable = true;
        Isbn = isbn;
        binding = new("Hardcover");
        available = true;
        hasOdinData = false;
        displayName = "";
        currentOdinData = new();
        CurrentOdinData.UpdateRequested += (sender, e) =>
            OdinUpdateRequested?.Invoke(this, (OdinDataViewModel)sender!);
        var bookService = ServiceHelper.GetService<BookService>();
        bindingOptions = bookService?.BookBindings.Select(b => $"{b}").ToList() ?? [];

        LoadBookDetails();
    }

    [RelayCommand]
    public void LoadBookDetails()
    {
        var bookService = ServiceHelper.GetService<BookService>();

        var bookInfo = bookService.BooksCache.FirstOrDefault(bk => bk.isbns.Any(item => item.isbn == Isbn));
        if (bookInfo is null) return;

        var isbn = bookInfo.isbns.FirstOrDefault(item => item.isbn == Isbn);
        if (isbn is null)
            return;
        Binding = new(isbn.binding);
        DisplayName = $"{Isbn} [{Binding}]";

        Book.Title = bookInfo.title;
        Book.Id = bookInfo.id;
        Book.AuthorList = bookInfo.authors.DelimeteredList();
        Book.Edition = bookInfo.edition;
        Book.PublicationDate = bookInfo.publicationDate.ToDateTime(default);
        Book.Publisher = bookInfo.publisher;
        
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
        var bookService = ServiceHelper.GetService<BookService>();
        bindingOptions = bookService?.BookBindings.Select(b => $"{b}").ToList() ?? [];
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
        var bookService = ServiceHelper.GetService<BookService>();
        var logging = ServiceHelper.GetService<LocalLoggingService>();

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
        var bookService = ServiceHelper.GetService<BookService>();
        var logging = ServiceHelper.GetService<LocalLoggingService>();

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
        var bookService = ServiceHelper.GetService<BookService>();
        var logging = ServiceHelper.GetService<LocalLoggingService>();

        if (bookService is null)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Warning,
                "Book Service was not available to fetch Odin Data from...");
            return;
        }

        var details = await bookService.GetISBNDetails(Isbn,
            err => logging?.LogMessage(LocalLoggingService.LogLevel.Error, err.error));

        if (details is null)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Debug, $"Unable to retrive book details for ISBN: {Isbn}");
            return;
        }

        var data = details;
        BookId = data.bookInfo.id;
        CurrentOdinData = data.odinData is not null
            ? new(data.odinData) {BookId = BookId, Isbn = Isbn}
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
        vm ??= new(isbn);

        return vm;
    }

    public static IsbnViewModel Get(ISBNDetail model)
    {
        var vm = ViewModelCache.FirstOrDefault(isbn => isbn.Isbn == model.isbn);
        if (vm is null)
        {
            vm = new(new ISBNInfo(model.isbn, model.binding, true, model.odinData is not null));
            ViewModelCache.Add(vm);
        }
        var output = vm.Clone();
        output.Book = new(model.bookInfo);
        return output;
    }

    public static IsbnViewModel Get(ISBNInfo model)
    {
        var vm = ViewModelCache.FirstOrDefault(isbn => isbn.Isbn == model.isbn);
        if(vm is null)
        {
            vm = new(model);
            ViewModelCache.Add(vm);

            var bookService = ServiceHelper.GetService<BookService>();
            var book = bookService.BooksCache.FirstOrDefault(bk=>bk.isbns.Any(isbn => isbn.isbn == model.isbn));
            vm.Book = new(book ?? BookDetail.Empty);
        }
        return vm.Clone();
    }

    public IsbnViewModel Clone() => (IsbnViewModel)MemberwiseClone();

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}
public partial class OdinDataViewModel : 
    ObservableObject, 
    IDefaultValueViewModel<OdinDataViewModel>,
    IModelCarrier<OdinDataViewModel, OdinData>
{
    public Optional<OdinData> Model { get; private set; } = Optional<OdinData>.None();

    public string BookId { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;

    public event EventHandler<OdinData>? UpdateRequested;

    [ObservableProperty] private string plu = "";

    [ObservableProperty] private double cost;

    public string CostString
    {
        get => $"{Cost:C}";
        set => Cost = value.ConvertToCurrency();
    }

    public static OdinDataViewModel Empty => new();

    [ObservableProperty] private bool isCurrent;

    public OdinDataViewModel()
    {
        plu = "";
        cost = 0;
        isCurrent = false;
    }
    public OdinDataViewModel(OdinData data)
    {
        this.Model = Optional<OdinData>.Some(data);
        plu = data.plu;
        cost = data.price;
        isCurrent = data.current;
    }

    [RelayCommand]
    public void SaveChange()
    {
        if(Model is not null)
            UpdateRequested?.Invoke(this, Model.Reduce(OdinData.None));
    }

    public static OdinDataViewModel Get(OdinData model) => new(model);
}