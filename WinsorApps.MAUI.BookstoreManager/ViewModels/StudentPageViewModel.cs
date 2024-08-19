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

using AcademicSectionViewModel = WinsorApps.MAUI.Shared.ViewModels.SectionViewModel;

namespace WinsorApps.MAUI.BookstoreManager.ViewModels;

public partial class StudentPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly BookstoreManagerService _manager;
    private readonly LocalLoggingService _logging;

    [ObservableProperty] ObservableCollection<StudentCartViewModel> students = [];
    [ObservableProperty] StudentCartViewModel selectedCart = new();
    [ObservableProperty] string searchText = "";
    [ObservableProperty] ObservableCollection<StudentCartViewModel> searchResults = [];
    [ObservableProperty] bool showSearchResults;
    [ObservableProperty] bool showSelected;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public StudentPageViewModel(BookstoreManagerService manager, LocalLoggingService logging)
    {
        _manager = manager;
        _logging = logging;
    }

    public event EventHandler<ErrorRecord>? OnError;

    public async Task Initialize(ErrorAction onError)
    {
        Busy = true;
        BusyMessage = "Initializing";
        var allOrders = await _manager.GetStudentOrders(onError);
        var ordersByStudent = allOrders.SeparateByKeys(ord => ord.student);

        Dictionary<string, ImmutableArray<StudentSectionBookRequirements>> cache = [];
        foreach(var student in  ordersByStudent.Keys.Select(student => student.id)) 
        {
            var result = await _manager.GetStudentBookRequirements(student, OnError.DefaultBehavior(this));
            cache.Add(student, result);
        }

        Students = [.. ordersByStudent.Select(kvp => new StudentCartViewModel(UserViewModel.Get(kvp.Key), kvp.Value, cache[kvp.Key.id]))];

        foreach(var cart in Students)
        {
            cart.Selected += (_, _) =>
            {
                SelectedCart = cart;
                ShowSelected = cart.IsSelected;

                foreach (var c in Students)
                {
                    c.IsSelected = c.Student.Id == cart.Student.Id && cart.IsSelected;
                }
            };
        }

        Busy = false;
    }

    [RelayCommand]
    public void Search()
    {
        SearchResults = [.. Students.Where(student => student.Student.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase))];

        if(SearchResults.Count == 1)
        {
            SelectedCart = SearchResults.First();
        }

        ShowSearchResults = SearchResults.Count > 1;
    }

    [RelayCommand]
    public async Task SubmitSelectedCart()
    {
        var selected = SelectedCart.Sections
                .SelectMany(sec => sec.Cart.Where(req => req.IsSelected).Select(req => req.Isbn.Isbn))
                .ToArray();
        var result = await _manager.MarkCompletedOrders(SelectedCart.Student.Id, selected, OnError.DefaultBehavior(this));
        if (result.Length > 0)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Confirmed pickup of {selected.Length} books.");
            var requiredBooks = await _manager.GetStudentBookRequirements(SelectedCart.Student.Id, OnError.DefaultBehavior(this));
            SelectedCart.Sections = new(result.Select(item => new StudentSectionCartViewModel(item, requiredBooks.FirstOrDefault(sec => sec.sectionId == item.section.sectionId))));
        }
    }
}

public partial class StudentCartViewModel :
    ObservableObject,
    ISelectable<StudentCartViewModel>
{
    [ObservableProperty] UserViewModel student;

    [ObservableProperty] ObservableCollection<StudentSectionCartViewModel> sections = [];
    [ObservableProperty] bool isSelected;

    public StudentCartViewModel() { Student = UserViewModel.Empty; }
    public StudentCartViewModel(UserViewModel student, IEnumerable<StudentSectionBookOrder> sections, ImmutableArray<StudentSectionBookRequirements> requiredBooks)
    {
        this.student = student;
        Sections = [.. sections.Select(item => new StudentSectionCartViewModel(item, requiredBooks.FirstOrDefault(sec => sec.sectionId == item.section.sectionId)))];
    }

    public event EventHandler<StudentCartViewModel>? Selected;

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}


public partial class StudentSectionCartViewModel :
    ObservableObject,
    IErrorHandling
{
    [ObservableProperty] AcademicSectionViewModel section;
    [ObservableProperty] ObservableCollection<StudentBookRequestViewModel> cart = [];
    [ObservableProperty] ObservableCollection<IsbnViewModel> notSelectedIsbns = [];

    private readonly BookstoreManagerService _service = ServiceHelper.GetService<BookstoreManagerService>();

    public StudentSectionCartViewModel(StudentSectionBookOrder model, StudentSectionBookRequirements requiredBooks)
    {
        Section = AcademicSectionViewModel.Get(model.section);

        Cart = [.. model.selectedBooks.Select(b => new StudentBookRequestViewModel(b))];

        NotSelectedIsbns = [.. requiredBooks.studentSections
            .SelectMany(group => group.isbns)
            .Where(entry => cart.All(req => req.Isbn.Isbn != entry.isbn))
            .Select(IsbnViewModel.Get)];

        foreach(var isbn in NotSelectedIsbns)
        {
            isbn.Selected += async (_, _) =>
            {
                var result = await _service.EditStudentBookOrder(model.student.id, model.sectionId, [isbn.Isbn], OnError.DefaultBehavior(this));
                if(result.HasValue)
                {
                    Cart = [.. result.Value.selectedBooks.Select(b => new StudentBookRequestViewModel(b))];
                    NotSelectedIsbns.Remove(isbn);
                }
            };
        }
    }

    public event EventHandler<ErrorRecord>? OnError;
}

public partial class StudentBookRequestViewModel :
    ObservableObject,
    ISelectable<StudentBookRequestViewModel>
{
    [ObservableProperty] StudentOrderStatus status = "";
    [ObservableProperty] DateTime submitted;
    [ObservableProperty] IsbnViewModel isbn = IsbnViewModel.Empty;
    [ObservableProperty] bool isSelected;
    public StudentBookRequestViewModel(StudentBookRequest model)
    {
        Isbn = IsbnViewModel.Get(model.isbn);
        Status = model.status;
        Submitted = model.submitted;
        IsSelected = Status == StudentOrderStatus.Completed;
    }

    public event EventHandler<StudentBookRequestViewModel>? Selected;

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}