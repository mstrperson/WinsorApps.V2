using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.Bookstore.ViewModels;

public partial class SummerBookOrderListItemViewModel :
    ObservableObject
{
    [ObservableProperty] IsbnViewModel isbn;
    [ObservableProperty] int quantity;

    public event EventHandler? DeleteRequested;

    public static implicit operator SummerBookOrderListItemViewModel(SummerBookOrderListItem model)
    {
        var vm = new SummerBookOrderListItemViewModel() 
        { 
            Isbn = IsbnViewModel.Get(model.isbn), 
            Quantity = model.quantity 
        };

        vm.Isbn.LoadBookDetails();
        return vm;
    }

    [RelayCommand]
    public void Delete() => DeleteRequested?.Invoke(this, EventArgs.Empty);
}

public partial class SummerSectionViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    IModelCarrier<SummerSectionViewModel, SummerSection>
{
    private readonly TeacherBookstoreService _service = ServiceHelper.GetService<TeacherBookstoreService>();

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] UserViewModel teacher = UserViewModel.Empty;
    [ObservableProperty] CourseViewModel course = CourseViewModel.Empty;
    [ObservableProperty] string schoolYear = "";
    [ObservableProperty] DateTime submitted;
    [ObservableProperty] ObservableCollection<SummerBookOrderListItemViewModel> books = [];

    public Optional<SummerSection> Model { get; private set; } = Optional<SummerSection>.None();

    public static SummerSectionViewModel Get(SummerSection model)
    {
        var vm = new SummerSectionViewModel()
        {
            Teacher = UserViewModel.Get(model.teacher),
            Course = CourseViewModel.Get(model.course),
            SchoolYear = model.schoolYear,
            Submitted = model.submitted,
            Books = [.. model.books]
        };

        foreach(var item in vm.Books)
        {
            item.DeleteRequested += async (_, _) =>
            {
                vm.Busy = true;
                vm.BusyMessage = $"Removing {item.Isbn.DisplayName}...";
                await vm.Model.Map(section => vm._service.DeleteSummerBook(section, item.Isbn.Isbn, vm.OnError.DefaultBehavior(vm))).Reduce(Task.CompletedTask);
                vm.Busy = false;
            };
        }

        return vm;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        var model = Model.Reduce(new("", Teacher.Model.Reduce(UserRecord.Empty), Course.Model.Reduce(CourseRecord.Empty), SchoolYear, DateTime.Today, []));
        if (string.IsNullOrEmpty(model.id)) return;

        Model = await _service.GetSummerSection(model.id, OnError.DefaultBehavior(this));
        Books = [.. Model.Map(section => section.books).Reduce([])]; 
        
        foreach (var item in Books)
        {
            item.DeleteRequested += async (_, _) =>
            {
                Busy = true;
                BusyMessage = $"Removing {item.Isbn.DisplayName}...";
                await Model.Map(section => _service.DeleteSummerBook(section, item.Isbn.Isbn, OnError.DefaultBehavior(this))).Reduce(Task.CompletedTask);
                Busy = false;
            };
        }
    }
}

