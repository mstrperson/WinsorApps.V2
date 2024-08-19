using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Bookstore.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.BookstoreManager.ViewModels;

public partial class TeacherOrderViewModel :
    ObservableObject,
    ICachedViewModel<TeacherOrderViewModel, TeacherBookOrderDetail, BookstoreManagerService>,
    IDefaultValueViewModel<TeacherOrderViewModel>,
    IErrorHandling,
    IBusyViewModel
{
    private readonly BookstoreManagerService _managerService = ServiceHelper.GetService<BookstoreManagerService>();

    [ObservableProperty] SectionViewModel section = SectionViewModel.Empty;
    [ObservableProperty] ImmutableArray<BookRequestViewModel> bookRequests = [];

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public async Task Submit()
    {
        Busy = true;
        BusyMessage = "Submitting Book Order";
        if (string.IsNullOrEmpty(Section.Id))
        {
            await Section.CreateSection();
            if (string.IsNullOrEmpty(Section.Id))
                return;
        }

        await _managerService.CreateOrUpdateBookOrder(Section.Teacher.Id, Section.Id,
            new CreateTeacherBookOrderGroup(BookRequests.Select(req => 
                new CreateTeacherBookRequest(req.Isbn.Isbn, req.Quantity, req.FallOrFullYear, req.SpringOnly)).ToImmutableArray()), OnError.DefaultBehavior(this));
        Busy = false;
    }

    public static ConcurrentBag<TeacherOrderViewModel> ViewModelCache { get; private set; } = [];

    public static TeacherOrderViewModel Empty => new();

    public static TeacherOrderViewModel Get(TeacherBookOrderDetail model)
    {
        var vm = ViewModelCache.FirstOrDefault(ord => ord.Section.Id == model.section.id);
        if(vm is null)
        {
            vm = new()
            {
                Section = SectionViewModel.Get(model.section),
                BookRequests = model.books.Select(book => new BookRequestViewModel(book)).ToImmutableArray()
            };
            ViewModelCache.Add(vm);
        }

        return vm.Clone();
    }

    public static List<TeacherOrderViewModel> GetClonedViewModels(IEnumerable<TeacherBookOrderDetail> models)
    {
        List<TeacherOrderViewModel> result = [];
        foreach (var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(BookstoreManagerService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

        foreach(var teacher in service.OrdersByTeacher.Keys)
            _ = GetClonedViewModels(service.OrdersByTeacher[teacher]);
    }

    public TeacherOrderViewModel Clone() => (TeacherOrderViewModel)MemberwiseClone();
}

public partial class BookRequestOptionGroupViewModel :
    ObservableObject,
    ICachedViewModel<BookRequestOptionGroupViewModel, TeacherBookOrderGroup, BookstoreManagerService>
{
    [ObservableProperty] string groupId = "";
    [ObservableProperty] BookOrderOptionViewModel option = BookOrderOptionViewModel.Empty;
    [ObservableProperty] ImmutableArray<BookRequestViewModel> requests = [];

    public static ConcurrentBag<BookRequestOptionGroupViewModel> ViewModelCache { get; private set; } = [];

    public static BookRequestOptionGroupViewModel Get(TeacherBookOrderGroup model)
    {
        var vm = ViewModelCache.FirstOrDefault(grp => grp.GroupId == model.id);
        if (vm is null)
        {

            vm = new()
            {
                GroupId = model.id,
                Option = BookOrderOptionViewModel.Get(model.option),
                Requests = model.isbns.Select(req => new BookRequestViewModel(req)).ToImmutableArray()
            };
            ViewModelCache.Add(vm);
        }
        return vm.Clone();
    }

    public static List<BookRequestOptionGroupViewModel> GetClonedViewModels(IEnumerable<TeacherBookOrderGroup> models)
    {
        List<BookRequestOptionGroupViewModel> result = [];
        foreach (var model in models)
            result.Add(Get(model));
        return result;
    }

    public static async Task Initialize(BookstoreManagerService service, ErrorAction onError)
    {
        // Do Nothing, build this cache on demand.
        await Task.CompletedTask;
    }

    public BookRequestOptionGroupViewModel Clone() => (BookRequestOptionGroupViewModel)MemberwiseClone();
}

public partial class BookRequestViewModel :
    ObservableObject,
    ISelectable<BookRequestViewModel>
{
    [ObservableProperty] IsbnViewModel isbn = IsbnViewModel.Empty;
    [ObservableProperty] DateTime submitted;
    [ObservableProperty] int quantity;
    [ObservableProperty] bool fallOrFullYear = true;
    [ObservableProperty] bool springOnly = false;
    [ObservableProperty] string status = "";
    [ObservableProperty] string groupId = "";

    [ObservableProperty] bool isSelected; 

    public event EventHandler<BookRequestViewModel>? Selected;

    public BookRequestViewModel(IsbnViewModel selectedIsbn)
    {
        Isbn = selectedIsbn.Clone();
    }

    public BookRequestViewModel(TeacherBookRequest request)
    {
        Isbn = IsbnViewModel.Get(request.isbn);
        submitted = request.submitted;
        quantity = request.quantity;
        fallOrFullYear = request.fall;
        springOnly = request.spring;
        status = request.status;
        groupId = request.groupId;
    }

    [RelayCommand]
    public void ToggleTerm()
    {
        FallOrFullYear = !FallOrFullYear;
        SpringOnly = !FallOrFullYear;
    }

    public TeacherBookRequest GetRequest() => new(Isbn.Isbn, Submitted, Quantity, FallOrFullYear, !FallOrFullYear && SpringOnly, Status, GroupId);

    public void Select()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
            Selected?.Invoke(this, this);
    }
}
