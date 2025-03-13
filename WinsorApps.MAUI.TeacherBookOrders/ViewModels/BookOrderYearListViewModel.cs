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

public partial class BookOrderYearListViewModel :
    ObservableObject,
    IErrorHandling
{
    public event EventHandler<ErrorRecord>? OnError;

    private readonly TeacherBookstoreService _service;
    private readonly RegistrarService _registrar;

    [ObservableProperty] ObservableCollection<SchoolYearListItem> years = [];


    public BookOrderYearListViewModel(TeacherBookstoreService service, RegistrarService registrar)
    {
        _service = service;
        _registrar = registrar;

        years = [.. 
            _registrar.SchoolYears
            .Where(sy => 
                sy.endDate > DateOnly.FromDateTime(DateTime.Today) ||
                _service.MySections.Any(sec => sec.schoolYearId == sy.id) || 
                _service.SummerSections.ContainsKey(sy.label))
            .Select(SchoolYearListItem.Get)];
    }
}

public partial class SchoolYearListItem :
    ObservableObject,
    ISelectable<SchoolYearListItem>,
    IModelCarrier<SchoolYearListItem, SchoolYear>
{
    [ObservableProperty] bool isSelected;

    public event EventHandler<SchoolYearListItem>? Selected;

    public event EventHandler<ProtoSectionViewModel>? ProtoSectionSelected;
    public event EventHandler<SummerSectionViewModel>? SummerSectionSelected;

    [ObservableProperty] string schoolYear = "";
    [ObservableProperty] ObservableCollection<ProtoSectionListItem> fallSections = [];
    [ObservableProperty] ObservableCollection<ProtoSectionListItem> springSections = [];
    [ObservableProperty] ObservableCollection<SummerSectionListItem> summerSections = [];

    public Optional<SchoolYear> Model { get; private set; } = Optional<SchoolYear>.None();

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }

    public static SchoolYearListItem Get(SchoolYear model)
    {
        var service = ServiceHelper.GetService<TeacherBookstoreService>();

        var allSections = service.MySections.Where(sec => sec.schoolYearId == model.id).ToList();
        var allIds = allSections.Select(sec => sec.id).ToList();

        var vm = new SchoolYearListItem()
        {
            SchoolYear = model.label,
            Model = Optional<SchoolYear>.Some(model),
            FallSections = [.. 
                allSections
                .Where(section => section.fallOrFullYear)
                .Select(ProtoSectionListItem.Get)],
            SpringSections = [..
                allSections
                .Where(section => section.springOnly)
                .Select(ProtoSectionListItem.Get)],
            SummerSections = [..
                service.SummerSections[model.label]
                .Select(SummerSectionListItem.Get)]
        };

        foreach (var section in vm.FallSections.Union(vm.SpringSections).ToList())
        {
            section.Selected += (_, listItem) =>
            {
                var option = (Optional<ProtoSectionViewModel>) listItem;
                option.MapStruct(section =>
                {
                    vm.ProtoSectionSelected?.Invoke(vm, section);
                    return true;
                });
            };
        }

        foreach(var section in vm.SummerSections)
        {
            section.Selected += (_, listItem) =>
            {
                var option = (Optional<SummerSectionViewModel>)listItem;
                option.MapStruct(section =>
                {
                    vm.SummerSectionSelected?.Invoke(vm, section);
                    return true;
                });
            };
        }

        return vm;
    }
}

public partial class SummerSectionListItem :
    ObservableObject,
    ISelectable<SummerSectionListItem>,
    IModelCarrier<SummerSectionListItem, SummerSection>
{
    [ObservableProperty] bool isSelected;
    [ObservableProperty] CourseViewModel course = CourseViewModel.Empty;

    public event EventHandler<SummerSectionListItem>? Selected;

    public Optional<SummerSection> Model { get; private set; } = Optional<SummerSection>.None();

    public static implicit operator Optional<SummerSectionViewModel>(SummerSectionListItem listItem) =>
        listItem.Model.Map(section =>
        {
            var vm = SummerSectionViewModel.Get(section);
            return vm;
        });


    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }

    public static SummerSectionListItem Get(SummerSection model) => new()
    {
        Course = CourseViewModel.Get(model.course),
        Model = Optional<SummerSection>.Some(model)
    };
}

public partial class ProtoSectionListItem :
    ObservableObject,
    ISelectable<ProtoSectionListItem>,
    IModelCarrier<ProtoSectionListItem, ProtoSection>
{
    [ObservableProperty] bool isSelected;
    [ObservableProperty] CourseViewModel course = CourseViewModel.Empty;
    [ObservableProperty] bool fallOrFullYear;

    public event EventHandler<ProtoSectionListItem>? Selected;
    public Optional<ProtoSection> Model { get; private set; } = Optional<ProtoSection>.None();

    public static implicit operator Optional<ProtoSectionViewModel>(ProtoSectionListItem listItem) =>
        listItem.Model.Map(section => 
        {
            var vm = ProtoSectionViewModel.Get(section);
            vm.LoadBookOrders().SafeFireAndForget(e => e.LogException());
            return vm;
        });

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }


    public static ProtoSectionListItem Get(ProtoSection model) =>
        new()
        {
            Course = CourseViewModel.Get(model.course),
            Model = Optional<ProtoSection>.Some(model),
            FallOrFullYear = model.fallOrFullYear
        };
}
