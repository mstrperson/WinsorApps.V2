using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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
    IBusyViewModel,
    IAsyncInitService
{
    private readonly BookstoreManagerService _manager;
    private readonly LocalLoggingService _logging;
    private readonly RegistrarService _registrar;

    [ObservableProperty] ObservableCollection<UserViewModel> students = [];
    [ObservableProperty] StudentCartViewModel selectedCart = new();
    [ObservableProperty] string searchText = "";
    [ObservableProperty] ObservableCollection<UserViewModel> searchResults = [];
    [ObservableProperty] bool showSearchResults;
    [ObservableProperty] bool showSelected;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool ready;
    [ObservableProperty] double progress;
    [ObservableProperty] bool started;

    public StudentPageViewModel(BookstoreManagerService manager, LocalLoggingService logging, RegistrarService registrar)
    {
        _manager = manager;
        _logging = logging;
        _registrar = registrar;
    }

    public event EventHandler<ErrorRecord>? OnError;

    public async Task Initialize(ErrorAction onError)
    {
        await _manager.WaitForInit(onError);

        Busy = true;
        BusyMessage = "Initializing";
        Started = true;
        _ = await _manager.GetStudentOrders(onError);
        var students = _registrar.StudentList.Where(student => student.studentInfo.HasValue && student.studentInfo.Value.className.StartsWith("Class V"));
        Progress = 0.5;

        Students = [.. UserViewModel.GetClonedViewModels(students)];
        Progress = 1;
        foreach(var student in Students)
        {
            student.Selected += (_, _) =>
            {
                SelectedCart = new(student, _manager.StudentOrders.Where(so => so.student.id == student.Id));
                SelectedCart.Selected += (_, _) =>
                {
                    ShowSelected = false;
                    ShowSearchResults = false;
                    SearchText = "";
                };

                ShowSelected = student.IsSelected;

                foreach (var c in Students)
                {
                    c.IsSelected = c.Id == student.Id && student.IsSelected;
                }
            };
        }

        Ready = true;

        Busy = false;
    }

    [RelayCommand]
    public async Task DownloadAllOrders()
    {
        Busy = true;
        BusyMessage = "Downloading All Student Orders";

        await _manager.DownloadStudentBookOrders(OnError.DefaultBehavior(this),
            async pdfData =>
            {
                Busy = true;
                BusyMessage = "Saving Downloaded Data.";
                using MemoryStream ms = new(pdfData);
                var result = await FileSaver.Default.SaveAsync(_logging.DownloadsDirectory, "StudentOrders.zip", ms);
                if(result.IsSuccessful && Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    ProcessStartInfo psi = new("explorer.exe", result.FilePath) { UseShellExecute = true };
                    Process.Start(psi);
                }

                Busy = false;
            });
        Busy = false;
    }

    [RelayCommand]
    public async Task Refresh(ErrorAction onError)
    {
        await Initialize(onError);
    }

    [RelayCommand]
    public void Search()
    {
        SearchResults = [.. Students.Where(student => student.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase))];

        if(SearchResults.Count == 1)
        {
            var student = SearchResults.First();
            SelectedCart = new(student, _manager.StudentOrders.Where(so => so.student.id == student.Id));
            SelectedCart.Selected += (_, _) =>
            {
                ShowSelected = false;
                ShowSearchResults = false;
                SearchText = "";
            };
            ShowSelected = true;
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
            SelectedCart.Sections = new(result.Select(item => new StudentSectionCartViewModel(item)));
        }
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }
}

public partial class StudentCartViewModel :
    ObservableObject,
    ISelectable<StudentCartViewModel>
{
    [ObservableProperty] UserViewModel student;

    [ObservableProperty] ObservableCollection<StudentSectionCartViewModel> sections = [];
    [ObservableProperty] bool isSelected;

    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();
    private readonly BookstoreManagerService _manager = ServiceHelper.GetService<BookstoreManagerService>();

    public StudentCartViewModel() { Student = UserViewModel.Empty; }
    public StudentCartViewModel(UserViewModel student, IEnumerable<StudentSectionBookOrder> sections)
    {
        using DebugTimer _ = new($"Loading cart for {student.DisplayName}", _logging);
        this.student = student;
        Sections = [.. sections
            .Where(sec => !string.IsNullOrEmpty(sec.section.primaryTeacherId))
            .Select(item => new StudentSectionCartViewModel(item))];
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
    IErrorHandling,
    IBusyViewModel
{
    [ObservableProperty] AcademicSectionViewModel section;
    [ObservableProperty] ObservableCollection<StudentBookRequestViewModel> cart = [];
    [ObservableProperty] ObservableCollection<IsbnViewModel> notSelectedIsbns = [];
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    private StudentSectionBookOrder _model;

    private readonly BookstoreManagerService _service = ServiceHelper.GetService<BookstoreManagerService>();

    public StudentSectionCartViewModel(StudentSectionBookOrder model)
    {
        _model = model;
        Section = AcademicSectionViewModel.Get(model.section);

        Cart = [.. model.selectedBooks.DistinctBy(req => req.isbn).Select(b => new StudentBookRequestViewModel(b))];

        foreach(var item in Cart)
        {
            item.DeleteRequested += async (_, _) =>
            {
                Busy = true;
                BusyMessage = $"Removing {item.Isbn.Book.Title}";
                Cart.Remove(item);
                var isbns = Cart.Select(req => req.Isbn.Isbn);
                var result = await _service.EditStudentBookOrder(_model.student.id, _model.sectionId, isbns, OnError.DefaultBehavior(this));
                if(!result.HasValue)
                {
                    Cart.Add(item);  // there was an error.
                }
                await LoadRequirements();
                Busy = false;
            };
        }
        LoadRequirements().SafeFireAndForget(e => e.LogException());
    }

    [RelayCommand]
    public async Task LoadRequirements()
    {
        Busy = true;
        BusyMessage = "Loading Books";
        var collection = await _service.GetStudentBookRequirements(_model.student.id, OnError.DefaultBehavior(this));
        if(collection.Length == 0)
        {
            Busy = false;
            return;
        }
        var sectionId = Section.Model.Reduce(Services.Global.Models.SectionRecord.Empty).sectionId;
        var requiredBooks = collection.FirstOrDefault(sec => sec.sectionId == sectionId);
        if (requiredBooks != default)
        {
            NotSelectedIsbns = [.. requiredBooks.studentSections
                .SelectMany(group => group.isbns)
                .Where(entry => Cart.All(req => req.Isbn.Isbn != entry.isbn))
                .Select(IsbnViewModel.Get)];

            foreach (var isbn in NotSelectedIsbns)
            {
                isbn.Selected += async (_, _) =>
                {
                    Busy = true;
                    BusyMessage = "Adding to Cart";
                    var result = await _service.EditStudentBookOrder(_model.student.id, _model.sectionId, [.. Cart.Select(item => item.Isbn.Isbn), isbn.Isbn], OnError.DefaultBehavior(this));
                    if (result.HasValue)
                    {
                        Cart = [.. result.Value.selectedBooks.Select(b => new StudentBookRequestViewModel(b))];
                        NotSelectedIsbns.Remove(isbn);
                    }
                    Busy = false;
                };
            }
        }
        Busy = false;
    }

    public event EventHandler<ErrorRecord>? OnError;
}

public partial class StudentBookRequestViewModel :
    ObservableObject,
    ISelectable<StudentBookRequestViewModel>
{
    private readonly BookService _books = ServiceHelper.GetService<BookService>();

    [ObservableProperty] StudentOrderStatus status = StudentOrderStatus.Submitted;
    [ObservableProperty] DateTime submitted;
    [ObservableProperty] IsbnViewModel isbn = IsbnViewModel.Empty;
    [ObservableProperty] bool isSelected;

    public event EventHandler<StudentBookRequestViewModel>? DeleteRequested;

    public StudentBookRequestViewModel(StudentBookRequest model)
    {
        var bookInfo = _books.BooksCache.FirstOrDefault(book => book.isbns.Any(isbn => isbn.isbn == model.isbn));
        if (bookInfo != default)
        {
            var info = bookInfo.isbns.FirstOrDefault(isbn => isbn.isbn == model.isbn);
            Isbn = new(info);
        }
        else
        {
            Isbn = IsbnViewModel.Get(model.isbn);
        }
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

    [RelayCommand]
    public void Delete() => DeleteRequested?.Invoke(this, this);
}