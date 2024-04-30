using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class IsbnViewModel : ObservableObject
{
    public event EventHandler<OdinDataViewModel>? OdinUpdateRequested;
    public event EventHandler<IsbnViewModel>? IsbnUpdateRequested;

    [ObservableProperty] bool editable;

    [ObservableProperty] string isbn;

    [ObservableProperty] BookBindingViewModel binding;

    [ObservableProperty] bool available;

    public string AvailableString => Available ? "Available" : "Not Available";

    [ObservableProperty] string displayName;

    [ObservableProperty] bool hasOdinData;

    [ObservableProperty] OdinDataViewModel currentOdinData;

    [ObservableProperty] ImmutableArray<string> bindingOptions;

    [ObservableProperty] string bookId = "";

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
}public partial class OdinDataViewModel : ObservableObject
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

    [ObservableProperty]
    bool? isCurrent;

    public OdinDataViewModel()
    {
        plu = "";
        cost = 0;
        isCurrent = null;
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