using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    IEmptyViewModel<TeacherOrderViewModel>
{
    private readonly BookstoreManagerService _managerService = ServiceHelper.GetService<BookstoreManagerService>();

    [ObservableProperty] SectionViewModel section = IEmptyViewModel<SectionViewModel>.Empty;
    [ObservableProperty] ImmutableArray<BookRequestViewModel> bookRequests = [];

    [RelayCommand]
    public async Task Submit()
    {
        if(string.IsNullOrEmpty(Section.Id))
            

        await _managerService.CreateOrUpdateBookOrder(Section.Teacher.Id, Section.Id, )
    }

    public static ConcurrentBag<TeacherOrderViewModel> ViewModelCache { get; private set; } = [];
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

public partial class BookRequestViewModel :
    ObservableObject
{
    [ObservableProperty] IsbnViewModel isbn = IEmptyViewModel<IsbnViewModel>.Empty;
    [ObservableProperty] DateTime submitted;
    [ObservableProperty] int quantity;
    [ObservableProperty] bool fallOrFullYear = true;
    [ObservableProperty] bool springOnly = false;
    [ObservableProperty] string status = "";
    [ObservableProperty] string groupId = "";

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
}
