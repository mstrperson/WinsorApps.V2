
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

    public OptionalStruct<ProtoSectionModel> Model { get; private set; } = OptionalStruct<ProtoSectionModel>.None();

    private ProtoSectionViewModel() { }

    [RelayCommand]
    public async Task LoadBookOrders()
    {
        var sectionId = Model.MapObject(ps => ps.id).Reduce("");
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
        Model = OptionalStruct<ProtoSectionModel>.Some(model),
        Course = CourseViewModel.Get(model.course),
        Teacher = UserViewModel.Get(_registrar.AllUsers.First(u => u.id == model.teacherId)),
        SchoolYear = _registrar.SchoolYears.First(sy => sy.id == model.schoolYearId).label
    };
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

    public OptionalStruct<TeacherBookOrderGroup> Model { get; private set; } = OptionalStruct<TeacherBookOrderGroup>.None();

    public static BookOrderGroupViewModel Get(TeacherBookOrderGroup model)
    {
        var vm = new BookOrderGroupViewModel()
        {
            Model = OptionalStruct<TeacherBookOrderGroup>.Some(model),
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


    public OptionalStruct<TeacherBookRequest> Model { get; private set;} = OptionalStruct<TeacherBookRequest>.None();


    public static BookOrderEntryViewModel Get(TeacherBookRequest model)
    {
        var vm = new BookOrderEntryViewModel()
        {
            Model = OptionalStruct<TeacherBookRequest>.Some(model),
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
