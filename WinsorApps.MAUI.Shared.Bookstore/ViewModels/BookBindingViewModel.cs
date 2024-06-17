using CommunityToolkit.Mvvm.ComponentModel;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class BookBindingViewModel : 
    ObservableObject
{
    [ObservableProperty]
    string binding;

    [ObservableProperty]
    private string id = "";

    private readonly BookBinding? _binding;

    public BookBindingViewModel(BookBinding binding)
    {
        _binding = binding;
        id = binding.id;
        this.binding = binding.binding;
    }

    public override string ToString() => Binding;

    public BookBindingViewModel(string binding)
    {
        this.binding = binding;
        BookService service = ServiceHelper.GetService<BookService>();
        LocalLoggingService logging = ServiceHelper.GetService<LocalLoggingService>();

        var temp = service.BookBindings.FirstOrDefault(b => b.binding.ToLowerInvariant() == binding.ToLowerInvariant());
        if(temp is null)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Warning, $"{binding} is not a valid binding found in the Book Service...");
            return;
        }

        _binding = temp;
    }
}