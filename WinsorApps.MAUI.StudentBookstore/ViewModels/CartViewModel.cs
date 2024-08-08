using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Bookstore.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using SectionRecord = WinsorApps.Services.Global.Models.SectionRecord;

namespace WinsorApps.MAUI.StudentBookstore.ViewModels;

public partial class MyCartViewModel :
    ObservableObject
{
    private readonly StudentBookstoreService _bookstore;
    private readonly RegistrarService _registrar;
    private readonly LocalLoggingService _logging;

    public MyCartViewModel(StudentBookstoreService bookstore, LocalLoggingService logging, RegistrarService registrar)
    {
        _bookstore = bookstore;
        _logging = logging;
        _registrar = registrar;
    }

    [ObservableProperty] ObservableCollection<SectionCartViewModel> myCart = [];
    [ObservableProperty] SectionCartViewModel selectedSection = SectionCartViewModel.Empty;
    [ObservableProperty] bool showSelected;

    public async Task Initialize(ErrorAction onError)
    {
        await _bookstore.WaitForInit(onError);
        _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Initializing MyCart");

        MyCart = [.. _registrar.MyAcademicSchedule.Select(sec => new SectionCartViewModel(sec.sectionId))];

        foreach(var cart in MyCart)
        {
            // What happens when you 
            cart.Selected += (_, _) =>
            {
                // if you're Selecting this cart, then display it.
                // if you're De-selecting it, hide everything.
                SelectedSection = cart.IsSelected ? cart : SectionCartViewModel.Empty;
                ShowSelected = cart.IsSelected;
                
                // Only one cart may be selected at a time.
                foreach(var c in MyCart)
                {
                    if (c.Section.Model.Reduce(SectionRecord.Empty).sectionId != cart.Section.Model.Reduce(SectionRecord.Empty).sectionId)
                        c.IsSelected = false;
                }
            };
        }
    }

    [RelayCommand]
    public async Task SubmitAll()
    {
        foreach(var section in MyCart)
        {
            if (section.HasChanges)
                await section.SubmitOrder();
        }
    }
}

public partial class SectionCartViewModel :
    ObservableObject,
    IErrorHandling,
    ISelectable<SectionCartViewModel>,
    IDefaultValueViewModel<SectionCartViewModel>,
    IBusyViewModel
{
    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();
    private readonly StudentBookstoreService _bookstore = ServiceHelper.GetService<StudentBookstoreService>();
    
    [ObservableProperty] SectionViewModel section = SectionViewModel.Empty;
    [ObservableProperty] ObservableCollection<BookRequestViewModel> requestedBooks = [];
    [ObservableProperty] ObservableCollection<OptionGroupViewModel> requiredBooks = [];
    [ObservableProperty] bool hasNoBooks;
    [ObservableProperty] bool hasChanges;
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] private int numbooks;

    public static SectionCartViewModel Empty => new();

    private SectionCartViewModel() { }
    public SectionCartViewModel(string sectionId, bool fall = true)
    {
        var sectionModel = _registrar.MyAcademicSchedule.FirstOrDefault(sec => sec.sectionId == sectionId);
        Section = SectionViewModel.Get(sectionModel);
        if(_bookstore.BookOrdersBySection.ContainsKey(sectionId))
        {
            LoadRequestedBooks(_bookstore.BookOrdersBySection[sectionId].selectedBooks);
        }
        var bookstoreVM = ServiceHelper.GetService<StudentBookstoreViewModel>();

        var semesterBookList = (fall ? _bookstore.FallBookList : _bookstore.SpringBookList).schedule.FirstOrDefault(list => list.sectionId == sectionId);
        if (semesterBookList.sectionId == sectionId)
            RequiredBooks = [..semesterBookList.studentSections.Select(group => new OptionGroupViewModel(group))];

        foreach(var group in RequiredBooks)
        {
            foreach (var isbn in group.Books)
                // By invoking the SelectCommand on any given IsbnViewModel in RequiredBooks
                // Add a request for the book to MyCart, if it isn't already there~
                isbn.Selected += (_, selected) =>
                {
                    if (RequestedBooks.Any(book => book.Isbn.Isbn == selected.Isbn))
                    {
                        // This book is already in MyCart!
                        return;
                    }


                    var newRequest = BookRequestViewModel.Create(selected);
                    newRequest.Selected += (_, toRemove) =>
                    {
                        RequestedBooks.Remove(newRequest);
                    };
                    RequestedBooks.Add(newRequest);
                    HasChanges = true;
                };
        }

        numbooks = requiredBooks.Select(group =>
            group.Option.StartsWith("ch", StringComparison.InvariantCultureIgnoreCase) ? 1 : group.Books.Count).Sum();
        HasNoBooks = !RequiredBooks.Any();
    }

    private void LoadRequestedBooks(IEnumerable<StudentBookRequest> requests)
    {
        RequestedBooks = [.. requests.Select(BookRequestViewModel.Get)];
        // By invoking the SelectCommand on a BookRequestViewModel
        // Remove that request from MyCart.
        foreach (var request in RequestedBooks)
            request.Selected += (_, toRemove) =>
            {
                RequestedBooks.Remove(request);
                HasChanges = true;
            };
    }

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<SectionCartViewModel>? Selected;

    /// <summary>
    /// Invoke this Command to submit the current changes to your cart 
    /// for this class only.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    public async Task SubmitOrder()
    {
        Busy = true;
        var result = await _bookstore.CreateOrUpdateBookOrder(Section.Model.Reduce(SectionRecord.Empty).sectionId, RequestedBooks.Select(request => request.Isbn.Isbn), OnError.DefaultBehavior(this));
        if (!result.HasValue)
        {
            Busy = false;
            return; // there was an error
        }

        HasChanges = false;
        Busy = false;
    } 

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class BookRequestViewModel :
    ObservableObject,
    IModelCarrier<BookRequestViewModel, StudentBookRequest>,
    ISelectable<BookRequestViewModel>
{
    [ObservableProperty] DateTime submitted;
    [ObservableProperty] OrderStatusViewModel status = OrderStatusViewModel.Get("");
    [ObservableProperty] IsbnViewModel isbn = IsbnViewModel.Empty;
    [ObservableProperty] bool isSelected;

    public event EventHandler<BookRequestViewModel>? Selected;

    
    public OptionalStruct<StudentBookRequest> Model { get; private set; } = OptionalStruct<StudentBookRequest>.None();

    private BookRequestViewModel() { }

    public static BookRequestViewModel Create(IsbnViewModel isbn) => new()
    {
        Model = OptionalStruct<StudentBookRequest>.Some(new(DateTime.Now, "", isbn.Isbn)),
        Submitted = DateTime.Now,
        Isbn = isbn
    };

    public static BookRequestViewModel Get(StudentBookRequest model) => new()
    {
        Model = OptionalStruct<StudentBookRequest>.Some(model),
        Isbn = IsbnViewModel.Get(model.isbn),
        Submitted = model.submitted,
        Status = OrderStatusViewModel.Get(model.status)
    };

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class OrderStatusViewModel :
    ObservableObject,
    ICachedViewModel<OrderStatusViewModel, OrderStatus, StudentBookstoreService>
{
    [ObservableProperty] string id = "";
    [ObservableProperty] string label = "";
    [ObservableProperty] string description = "";

    private OrderStatusViewModel() { }

    public static ConcurrentBag<OrderStatusViewModel> ViewModelCache { get; private set; } = [];

    public static OrderStatusViewModel Get(string status) => ViewModelCache.FirstOrDefault(st => st.label.Equals(status, StringComparison.InvariantCultureIgnoreCase))?.Clone() ?? new();

    public static OrderStatusViewModel Get(OrderStatus model)
    {
        var vm = ViewModelCache.FirstOrDefault(item => item.Id == model.id);
        if (vm is not null) 
            return vm.Clone();

        vm = new()
        {
            Id = model.id,
            Label = model.label,
            Description = model.description
        };

        ViewModelCache.Add(vm);
        return vm.Clone();
    }

    public static List<OrderStatusViewModel> GetClonedViewModels(IEnumerable<OrderStatus> models) =>
        models.Select(Get).ToList();

    public static async Task Initialize(StudentBookstoreService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);
        _ = GetClonedViewModels(service.OrderStatusOptions);
    }

    public OrderStatusViewModel Clone() => (OrderStatusViewModel)MemberwiseClone();
}
