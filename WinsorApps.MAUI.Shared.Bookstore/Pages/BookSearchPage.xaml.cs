using WinsorApps.MAUI.Shared.Bookstore.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.Pages;

public partial class BookSearchPage : ContentPage
{
    public event EventHandler<BookViewModel>? EditBookRequested;

    public BookSearchPage(BookService bookService, LocalLoggingService logging)
    {
        InitializeComponent();
        var vm = new BookSearchViewModel();
        this.BindingContext = vm;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        EditBookRequested?.Invoke(this, new());
    }
}