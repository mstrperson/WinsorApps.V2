
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Bookstore.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherBookOrders.ViewModels;
[Obsolete]
public partial class TeacherBookOrderPageViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherBookstoreService _bookstore;

    [ObservableProperty]
    ObservableCollection<BookOrderYearCollectionViewModel> years = [];

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public TeacherBookOrderPageViewModel()
    {
        _bookstore = ServiceHelper.GetService<TeacherBookstoreService>()!;
        var registrar = ServiceHelper.GetService<RegistrarService>()!;

        var schoolYears = registrar.SchoolYears
            .Where(sy => 
                sy.id == _bookstore.CurrentBookOrderYear.id || 
                _bookstore.MyOrders.Any(ord => ord.schoolYearId == sy.id))
            .ToImmutableArray();

        years = [.. schoolYears.Select(sy => new BookOrderYearCollectionViewModel(sy))];

        foreach(var year in Years)
        {
            year.OnError += (sender, err) => OnError?.Invoke(sender, err);
            year.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }
    }

    public event EventHandler<ErrorRecord>? OnError;
}


[Obsolete]
public partial class BookOrderYearCollectionViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherBookstoreService _bookstore;

    [ObservableProperty]
    ObservableCollection<BookOrderViewModel> fallOrders = [];

    [ObservableProperty]
    ObservableCollection<BookOrderViewModel> springOrders = [];

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty]
    string schoolYear = "";

    [ObservableProperty]
    bool isVisible = true;

    [ObservableProperty]
    bool isCurrent;

    private readonly SchoolYear _schoolYear;

    public event EventHandler<ErrorRecord>? OnError;

    public BookOrderYearCollectionViewModel(SchoolYear schoolYear)
    {
        _bookstore = ServiceHelper.GetService<TeacherBookstoreService>()!;
        var registrar = ServiceHelper.GetService<RegistrarService>()!;
        fallOrders = [.. _bookstore.MyOrders
            .Where(ord => ord.schoolYearId == schoolYear.id && (ord.Fall || (!ord.Fall && !ord.Spring)))
            .Select(ord => 
                new BookOrderViewModel(ord))
            ];
        springOrders = [.. _bookstore.MyOrders
            .Where(ord => ord.schoolYearId == schoolYear.id && ord.Spring)
            .Select(ord => 
                new BookOrderViewModel(ord))
            ];
        isVisible = schoolYear == registrar.SchoolYears.First();
        SchoolYear = schoolYear.label;
        _schoolYear = schoolYear;
        isCurrent = schoolYear == _bookstore.CurrentBookOrderYear;
    }

    [RelayCommand]
    public void ToggleVisible() => IsVisible = !IsVisible;

    [RelayCommand]
    public void RefreshCache()
    {
        FallOrders = [.. _bookstore.MyOrders
            .Where(ord => 
                ord.schoolYearId == _schoolYear.id && 
                (ord.Fall || (!ord.Fall && !ord.Spring)))
            .Select(ord => new BookOrderViewModel(ord))
            ];
        SpringOrders = [.. _bookstore.MyOrders
            .Where(ord => ord.schoolYearId == _schoolYear.id && ord.Spring)
            .Select(ord => new BookOrderViewModel(ord))
            ];

        foreach(var order in FallOrders.Union(SpringOrders))
        {
            order.OnError += (sender, err) => OnError?.Invoke(sender, err);
            order.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }
    }
}

[Obsolete]
public partial class BookOrderViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    [ObservableProperty]
    string courseName = "";

    [ObservableProperty]
    string sectionId = "";

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty]
    ObservableCollection<BookRequestViewModel> books = [];

    public string Description => $"{CourseName} [{Books.Count} Books Requested]";

    [ObservableProperty]
    string schoolYear = "";

    public bool ShowGroups => Groups.Count > 1;
    public bool ShowBooks => Groups.Count <= 1;

    [ObservableProperty]
    ObservableCollection<RequestGroupViewModel> groups = [];

    private readonly TeacherBookstoreService _bookstoreService = 
        ServiceHelper.GetService<TeacherBookstoreService>();

    public async Task<List<RequestGroupViewModel>> GetGroups()
    {
        List<RequestGroupViewModel> result = new();

        var groups = Books.Select(req => req.GroupId).Distinct();
        foreach (var groupId in groups)
        {
            if (string.IsNullOrEmpty(groupId))
                result.Add(
                    new(new TeacherBookRequestGroup("", "Not Grouped",
                    Books.Where(bk => bk.GroupId == groupId)
                         .Select(vm => (TeacherBookRequest)vm)
                         .ToImmutableArray())));

            else
            {
                var grp = await _bookstoreService
                    .GetGroupDetails(groupId, OnError.DefaultBehavior(this));
                if (grp.HasValue)
                    result.Add(new(grp.Value));
            }
        }

        return result;
    }

    public BookOrderViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>()!;
        sectionId = "";
        schoolYear = registrar.SchoolYears.First().label;
        books = [];
        courseName = _bookstoreService.MySections
            .First(sec => sec.id == SectionId).course.displayName;
        groups = [];
        canEdit = true;
    }

    [ObservableProperty]
    bool canEdit;

    public BookOrderViewModel(TeacherBookOrder order)
    {
        sectionId = order.protoSectionId;
        var registrar = ServiceHelper.GetService<RegistrarService>()!;
        schoolYear = registrar.GetSchoolYear(order.schoolYearId).label;
        canEdit = order.schoolYearId == _bookstoreService.CurrentBookOrderYear.id;
        books = [.. order.books
            .Distinct()
            .Select(req => new BookRequestViewModel(req, CanEdit))];
        courseName = _bookstoreService.MySections
            .First(sec => sec.id == SectionId).course.displayName;
        groups = [];
        var getGroupsTask = GetGroups();
        getGroupsTask.WhenCompleted(() =>
        {
            Groups = [.. getGroupsTask.Result];
        });
        model = order;
    }

    private TeacherBookOrder model;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? Deleted;

    public static implicit operator TeacherBookOrder(BookOrderViewModel vm) => vm.model;
}

[Obsolete]
public partial class BookRequestViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    public BookDetail Book { get; protected set; }

    private readonly TeacherBookstoreService bookstoreService = 
        ServiceHelper.GetService<TeacherBookstoreService>();
    private readonly BookService bookService = 
        ServiceHelper.GetService<BookService>();
    private readonly LocalLoggingService loggingService = 
        ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty]
    string isbn = "";

    [ObservableProperty]
    string binding = "";

    [ObservableProperty]
    DateTime submitted;

    [ObservableProperty]
    int quantity;

    [ObservableProperty]
    bool fall;

    [ObservableProperty]
    bool spring;
    [ObservableProperty]
    string status = "";
    [ObservableProperty]
    string groupId = "";

    [ObservableProperty]
    bool canEdit;

    [ObservableProperty]
    private string groupLabel = "";

    public event EventHandler<ErrorRecord>? OnError;

    private async Task UpdateGroupLabel()
    {
        if (string.IsNullOrEmpty(GroupId))
            return;
        var grp = await bookstoreService
            .GetGroupDetails(GroupId, OnError.DefaultBehavior(this));

        if (grp.HasValue)
        {
            GroupLabel = grp.Value.option;
        }
    }

    public async Task Initialize()
    {
        while (!bookstoreService.Ready || !bookService.Ready)
        {
            await Task.Delay(250);
        }

        await UpdateGroupLabel();

        var result = await bookService
            .GetISBNDetails(Isbn, OnError.DefaultBehavior(this));

        if (!result.HasValue)
        {
            loggingService.LogMessage(
                LocalLoggingService.LogLevel.Debug, 
                $"Searching for {Isbn} returned no results....");
            return;
        }

        Binding = result.Value.binding;
    }

    public BookRequestViewModel(TeacherBookRequest request, bool canEdit = true)
    {
        this.isbn = request.isbn;
        this.submitted = request.submitted;
        this.quantity = request.quantity;
        this.spring = request.spring;
        this.status = request.status;
        this.groupId = request.groupId;
        this.fall = request.fall;

        Book = bookService.BooksCache
            .First(bk => bk.isbns.Any(entry => entry.isbn == Isbn));

        binding = "";

        groupLabel = string.IsNullOrEmpty(request.groupId) ? 
            string.Empty : "Options Available";
        this.canEdit = canEdit;
    }

    public override string ToString() => $"{Book}";

    public static implicit operator TeacherBookRequest(BookRequestViewModel vm) =>
        new(vm.Isbn, vm.Submitted, vm.Quantity, vm.Fall, vm.Spring, vm.Status, vm.GroupId);
}

[Obsolete]
public partial class BookRequestEditorViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherBookstoreService _bookstoreService = 
        ServiceHelper.GetService<TeacherBookstoreService>();
    private readonly LocalLoggingService _logging = 
        ServiceHelper.GetService<LocalLoggingService>();

    public BookRequestEditorViewModel(BookDetail bookDetail)
    {
        book = BookViewModel.Get(bookDetail);
        groupingLabel = "All Required";
        canEdit = true;
        quantity = 0;
        selectedIsbns = new();
        fall = true;
        groupId = "";
        spring = false;
    }

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty]
    bool canEdit;

    public BookRequestEditorViewModel(BookDetail bookDetail, TeacherBookOrder order)
    {
        canEdit = order.schoolYearId == _bookstoreService.CurrentBookOrderYear.id;
        groupId = order.books.Any() ? order.books.First().groupId : "";

        groupingLabel = "All Required";

        if (!string.IsNullOrEmpty(groupId))
        {
            var groupTask = _bookstoreService.GetGroupDetails(GroupId, _logging.LogError);
            groupTask.WhenCompleted(() =>
            {
                var group = groupTask.Result;
                if (!group.HasValue)
                    return;

                GroupingLabel = group.Value.option;
            });
        }

        book = BookViewModel.Get(bookDetail);

        if (!order.books.Any(bk => bookDetail.isbns.Any(ent => ent.isbn == bk.isbn)))
        {
            quantity = 0;
            selectedIsbns = new();
            fall = true;
            spring = false;
            return;
        }

        var selected = order.books
            .Where(bk => bookDetail.isbns.Any(ent => ent.isbn == bk.isbn));

        selectedIsbns = [.. selected.Select(ent => ent.isbn)];
        quantity = selected.First().quantity;
        fall = selected.First().fall;
        spring = selected.First().spring;
    }

    [ObservableProperty]
    BookViewModel book;

    public string Title => $"{Book}";

    [ObservableProperty]
    public int quantity;

    public string QuantityString
    {
        get => $"{Quantity}";
        set => Quantity = int.TryParse(value, out int q) ? q : 0;
    }

    [ObservableProperty]
    ObservableCollection<string> selectedIsbns = [];

    [ObservableProperty]
    bool fall;

    [ObservableProperty]
    bool spring;

    [ObservableProperty]
    string groupingLabel = "";

    [ObservableProperty]
    string groupId = "";

    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    public void ToggleGrouping()
    {
        var n = _bookstoreService.OrderOptions
            .Select(opt => opt.label)
            .ToList()
            .IndexOf(GroupingLabel);

        GroupingLabel = _bookstoreService
            .OrderOptions[(n + 1) % _bookstoreService.OrderOptions.Length].label;
    }

    [RelayCommand]
    public void UpdateSpringAccordingToFall()
    {
        if (Fall)
            Spring = false;
    }

    [RelayCommand]
    public void UpdateFallAccordingToSpring()
    {
        if (Spring)
            Fall = false;
    }
}

[Obsolete]
public partial class RequestGroupViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty]
    string groupId = "";

    [ObservableProperty]
    string option = "";

    [ObservableProperty]
    ObservableCollection<BookRequestViewModel> bookReqeusts = [];

    public RequestGroupViewModel(TeacherBookRequestGroup group)
    {
        this.groupId = group.groupId;
        this.option = group.option;
        this.bookReqeusts = [.. group.requestedISBNs.Select(req => new BookRequestViewModel(req))];
        foreach(var request in BookReqeusts)
        {
            request.OnError += (sender, err) => OnError?.Invoke(sender, err);
            request.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }
    }

    public event EventHandler<ErrorRecord>? OnError;
}

[Obsolete]
public partial class BookOrderCollectionViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{


    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty]
    ObservableCollection<BookOrderViewModel> bookOrders = [];

    public BookOrderCollectionViewModel(SchoolYear? schoolYear = null)
    {
        TeacherBookstoreService bookstore = ServiceHelper.GetService<TeacherBookstoreService>();

        if (schoolYear.HasValue)
            bookOrders = [.. bookstore.MyOrders.Where(ord => ord.schoolYearId == schoolYear.Value.id).Select(order => new TeacherBookOrderDetail(
                bookstore.MySections.First(sec => sec.id == order.protoSectionId)!,
                order.books))
                .Select(ord => new BookOrderViewModel(ord))];
        else
            bookOrders = [.. bookstore.CurrentYearOrders
                .Select(order => 
                    new TeacherBookOrderDetail(
                        bookstore.MySections.First(sec => sec.id == order.protoSectionId)!,
                        order.books))
                .Select(ord => new BookOrderViewModel(ord))];

        foreach(var order in BookOrders)
        {
            order.OnError += (sender, err) => OnError?.Invoke(sender, err);
            order.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            order.Deleted += (_, _) => BookOrders.Remove(order);
        }
    }

    public event EventHandler<ErrorRecord>? OnError;

    public void AddBookOrder(TeacherBookOrderDetail order)
    {
        if (BookOrders.Any(ord => ord.SectionId == order.section.id))
        {
            var oldOrder = BookOrders.First(ord => ord.SectionId == order.section.id);
            BookOrders.Remove(oldOrder);
        }

        var newOrder = new BookOrderViewModel(order);
        newOrder.OnError += (sender, err) => OnError?.Invoke(sender, err);
        newOrder.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        newOrder.Deleted += (_, _) => BookOrders.Remove(newOrder);
        BookOrders.Add(newOrder);
    }
}