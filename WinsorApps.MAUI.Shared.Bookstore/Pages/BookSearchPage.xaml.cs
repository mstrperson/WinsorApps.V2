using WinsorApps.MAUI.Shared.Bookstore.ViewModels;

namespace WinsorApps.MAUI.Shared.Bookstore.Pages;

public partial class BookSearchPage : ContentPage
{
    public event EventHandler<BookViewModel>? EditBookRequested;


    public BookSearchViewModel ViewModel => (BookSearchViewModel)BindingContext;

    public BookSearchPage(BookSearchViewModel vm)
    {
        InitializeComponent();
        this.BindingContext = vm;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        EditBookRequested?.Invoke(this, new());
    }
}