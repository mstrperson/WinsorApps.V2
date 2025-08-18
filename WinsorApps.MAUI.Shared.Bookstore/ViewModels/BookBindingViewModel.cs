using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Concurrent;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class BookBindingViewModel : 
    ObservableObject
{
    [ObservableProperty] private string binding;

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
        var service = ServiceHelper.GetService<BookService>();
        var logging = ServiceHelper.GetService<LocalLoggingService>();

        var temp = service.BookBindings.FirstOrDefault(b => b.binding.Equals(binding, StringComparison.InvariantCultureIgnoreCase));
        if(temp is null)
        {
            logging?.LogMessage(LocalLoggingService.LogLevel.Warning, $"{binding} is not a valid binding found in the Book Service...");
            return;
        }

        _binding = temp;
    }
}

public partial class BookOrderOptionViewModel :
    ObservableObject,
    ICachedViewModel<BookOrderOptionViewModel, OrderOption, BookService>,
    IDefaultValueViewModel<BookOrderOptionViewModel>
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string label = "";
    [ObservableProperty] private string description = "";

    public static List<BookOrderOptionViewModel> ViewModelCache { get; private set; } = [];
    private static BookService BookService => ServiceHelper.GetService<BookService>();
    public static BookOrderOptionViewModel Empty => Get(BookService.OrderOptions.First());

    public static BookOrderOptionViewModel Get(string option)
    {
        var orderOption = BookService.OrderOptions.FirstOrDefault(opt => opt.label.Equals(option, StringComparison.InvariantCultureIgnoreCase)) ?? new("", "", "");
        if (string.IsNullOrEmpty(orderOption.id))
            return Empty;
        return Get(orderOption);
    }

    public static BookOrderOptionViewModel Get(OrderOption model)
    {
        var vm = ViewModelCache.FirstOrDefault(opt => opt.Id == model.id);
        if(vm is null)
        {
            vm = new() { Id = model.id, Description = model.description, Label = model.label };
            ViewModelCache.Add(vm);
        }
        return vm.Clone();
    }

    public static List<BookOrderOptionViewModel> GetClonedViewModels(IEnumerable<OrderOption> models)
    { 
        List<BookOrderOptionViewModel> result = [];

        foreach(var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(BookService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

        _ = GetClonedViewModels(service.OrderOptions);
    }

    public BookOrderOptionViewModel Clone() => (BookOrderOptionViewModel)MemberwiseClone();
}