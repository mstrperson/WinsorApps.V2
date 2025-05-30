﻿
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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
using ProtoSectionModel = WinsorApps.Services.Bookstore.Models.ProtoSection;

namespace WinsorApps.MAUI.TeacherBookOrders.ViewModels;


public partial class ProtoSectionViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    IModelCarrier<ProtoSectionViewModel, ProtoSectionModel>
{
    private static readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    private readonly TeacherBookstoreService _service = ServiceHelper.GetService<TeacherBookstoreService>();    

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] CourseViewModel course = CourseViewModel.Empty;
    [ObservableProperty] UserViewModel teacher = UserViewModel.Empty;
    [ObservableProperty] string schoolYear = "";
    [ObservableProperty] ObservableCollection<BookOrderGroupViewModel> bookOrders = [];
    [ObservableProperty] bool fallOrFullYear;

    public Optional<ProtoSectionModel> Model { get; private set; } = Optional<ProtoSectionModel>.None();

    private ProtoSectionViewModel() { }

    [RelayCommand]
    public async Task LoadBookOrders()
    {
        var sectionId = Model.Map(ps => ps.id).Reduce("");
        if (string.IsNullOrEmpty(sectionId)) return;

        var result = await _service.GetOrderGroups(sectionId, OnError.DefaultBehavior(this));
        BookOrders = [.. result.Select(BookOrderGroupViewModel.Get)];

        foreach(var group in BookOrders)
        {
            group.OnError += (sender, err) => OnError?.Invoke(sender, err);
            group.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            group.Deleted += (_, _) => BookOrders.Remove(group);
        }
    }

    public static ProtoSectionViewModel Get(ProtoSectionModel model) => new()
    {
        Model = Optional<ProtoSectionModel>.Some(model),
        Course = CourseViewModel.Get(model.course),
        Teacher = UserViewModel.Get(_registrar.AllUsers.First(u => u.id == model.teacherId)),
        SchoolYear = _registrar.SchoolYears.First(sy => sy.id == model.schoolYearId).label,
        FallOrFullYear = model.fallOrFullYear
    };
}

public partial class SelectedBookViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    IModelCarrier<SelectedBookViewModel, TeacherBookOrderGroup>
{
    private readonly TeacherBookstoreService _service = ServiceHelper.GetService<TeacherBookstoreService>();
    private static readonly BookService _bookService = ServiceHelper.GetService<BookService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? Deleted;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] BookViewModel book = BookViewModel.Empty;
    [ObservableProperty] ObservableCollection<IsbnViewModel> availableIsbns = [];
    [ObservableProperty] string option = "";
    [ObservableProperty] ObservableCollection<string> options;
    [ObservableProperty] IsbnViewModel isbnEditor = IsbnViewModel.Empty;

    private SelectedBookViewModel()
    {
        options = [.. _service.OrderOptions.Select(opt => opt.label)];
    }
    public Optional<TeacherBookOrderGroup> Model { get; private set; } = Optional<TeacherBookOrderGroup>.None();

    public static SelectedBookViewModel Get(TeacherBookOrderGroup model)
    {
        if (model.isbns.Count == 0)
            throw new InvalidDataException();

        var book = _bookService.BooksCache.FirstOrDefault(book => book.isbns.Any(ent => ent.isbn == model.isbns[0].isbn));

        var vm = new SelectedBookViewModel()
        {
            Model = Optional<TeacherBookOrderGroup>.Some(model),
            Option = model.option,
            Book = BookViewModel.Get(book ?? BookDetail.Empty),
            AvailableIsbns = [.. book?.isbns.Where(ent => ent.available).Select(IsbnViewModel.Get) ?? []]
        };

        foreach(var entry in vm.AvailableIsbns)
        {
            entry.IsSelected = model.isbns.Any(ord => ord.isbn == entry.Isbn);
        }

        return vm;
    }

    [RelayCommand]
    public async Task SaveNewISBN()
    {
        if (string.IsNullOrEmpty(IsbnEditor.Isbn))
            return;

        CreateISBN newISBN = new(IsbnEditor.Isbn, IsbnEditor.Binding.Id);

        var result = await _bookService.AddIsbnToBook(Book.Id, newISBN, OnError.DefaultBehavior(this));
        if (result is not null)
        {
            var book = result;
            Book = BookViewModel.Get(book);
            AvailableIsbns = [.. book.isbns.Where(ent => ent.available).Select(IsbnViewModel.Get)];

            foreach (var entry in AvailableIsbns)
            {
                entry.IsSelected = Model.Reduce(TeacherBookOrderGroup.Empty).isbns.Any(ord => ord.isbn == entry.Isbn);
            }
        }
    }
}

public partial class BookOrderGroupViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    IModelCarrier<BookOrderGroupViewModel, TeacherBookOrderGroup>
{
    private readonly TeacherBookstoreService _service = ServiceHelper.GetService<TeacherBookstoreService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? Deleted;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] string option = "";
    [ObservableProperty] ObservableCollection<string> options;
    [ObservableProperty] ObservableCollection<BookOrderEntryViewModel> orders = [];

    private BookOrderGroupViewModel()
    {
        options = [.. _service.OrderOptions.Select(opt => opt.label)];
    }

    public Optional<TeacherBookOrderGroup> Model { get; private set; } = Optional<TeacherBookOrderGroup>.None();

    public static BookOrderGroupViewModel Get(TeacherBookOrderGroup model)
    {
        var vm = new BookOrderGroupViewModel()
        {
            Model = Optional<TeacherBookOrderGroup>.Some(model),
            Option = model.option,
            Orders = [.. model.isbns.Select(BookOrderEntryViewModel.Get)]
        };

        return vm;
    }
}

public partial class BookOrderEntryViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    IModelCarrier<BookOrderEntryViewModel, TeacherBookRequest>
{
    public event EventHandler<ErrorRecord>? OnError;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] IsbnViewModel isbn = IsbnViewModel.Empty;
    [ObservableProperty] DateTime submitted;
    [ObservableProperty] int quantity;
    [ObservableProperty] bool fallOrFullYear = true;
    [ObservableProperty] bool springOnly;


    public Optional<TeacherBookRequest> Model { get; private set;} = Optional<TeacherBookRequest>.None();


    public static BookOrderEntryViewModel Get(TeacherBookRequest model)
    {
        var vm = new BookOrderEntryViewModel()
        {
            Model = Optional<TeacherBookRequest>.Some(model),
            Isbn = IsbnViewModel.Get(model.isbn),
            Submitted = model.submitted,
            Quantity = model.quantity,
            FallOrFullYear = model.fall,
            SpringOnly = model.spring
        };

        vm.Isbn.LoadBookDetails();

        return vm;
    }
}
