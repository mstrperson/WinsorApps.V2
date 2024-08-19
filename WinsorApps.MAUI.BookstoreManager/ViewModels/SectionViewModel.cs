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
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using SectionRecord = WinsorApps.Services.Bookstore.Models.SectionRecord;

namespace WinsorApps.MAUI.BookstoreManager.ViewModels;

public partial class SectionViewModel :
    ObservableObject,
    ICachedViewModel<SectionViewModel, SectionRecord, BookstoreManagerService>,
    IEmptyViewModel<SectionViewModel>,
    IErrorHandling,
    IBusyViewModel
{
    private static readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();
    private static readonly BookstoreManagerService _managerService = ServiceHelper.GetService<BookstoreManagerService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string schoolYearId = "";
    [ObservableProperty] CourseViewModel course = IEmptyViewModel<CourseViewModel>.Empty;
    [ObservableProperty] UserViewModel teacher = IEmptyViewModel<UserViewModel>.Empty;
    [ObservableProperty] DateTime created;
    [ObservableProperty] ImmutableArray<BookRequestOptionGroupViewModel> requestGroups = [];

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;

    public OptionalStruct<SectionRecord> Model { get; private set; } = OptionalStruct<SectionRecord>.None();

    public SectionViewModel()
    {

    }

    [RelayCommand]
    public async Task CreateSection()
    {
        if (string.IsNullOrEmpty(Course.Id) || string.IsNullOrEmpty(Teacher.Id))
            return;
        Busy = true;
        BusyMessage = $"Creating new Section of {Course.DisplayName} for {Teacher.DisplayName}";
        var result = await _managerService.CreateSectionForTeacher(Teacher.Id, Course.Id, OnError.DefaultBehavior(this));
        if (!result.HasValue)
        {
            Busy = false;
            return;
        }

        Model = OptionalStruct<SectionRecord>.Some(result.Value);
        this.Id = result.Value.id;
        this.SchoolYearId = result.Value.schoolYearId;
        Busy = false;
    }

    [RelayCommand]
    public async Task GetOrderGroups()
    {
        Busy = true;
        BusyMessage = "Getting Book Orders";
        var groups = await _managerService.GetGroupedOrders(Id, OnError.DefaultBehavior(this));
        RequestGroups = BookRequestOptionGroupViewModel.GetClonedViewModels(groups).ToImmutableArray();
        Busy = false;
    }

    public static ConcurrentBag<SectionViewModel> ViewModelCache { get; private set; } = [];

    public static SectionViewModel Get(SectionRecord model)
    {
        var vm = ViewModelCache.FirstOrDefault(sec => sec.Id == model.id);
        if(vm is null)
        {
            vm = new()
            {
                Id = model.id,
                SchoolYearId = model.schoolYearId,
                Course = CourseViewModel.Get(model.course),
                Teacher = UserViewModel.Get(_registrar.AllUsers.First(u => u.id == model.teacherId)),

                Created = model.createdTimeStamp,
                Model = OptionalStruct<SectionRecord>.Some(model)
            };
            ViewModelCache.Add(vm);
        }

        return vm.Clone();
    }

    public static List<SectionViewModel> GetClonedViewModels(IEnumerable<SectionRecord> models)
    {
        List<SectionViewModel> result = [];
        foreach(var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(BookstoreManagerService service, ErrorAction onError)
    {
        await _registrar.WaitForInit(onError);
        await service.WaitForInit(onError);

        service.OnCacheRefreshed += (_, _) =>
        {
            ViewModelCache = [];
            _ = GetClonedViewModels(service.ProtoSections);
        };

        _ = GetClonedViewModels(service.ProtoSections);
    }

    public SectionViewModel Clone() => (SectionViewModel)MemberwiseClone();
}

public partial class SectionByTeacherCollectionViewModel :
    ObservableObject,
    ICachedViewModel<SectionByTeacherCollectionViewModel, UserRecord, BookstoreManagerService>

{
    private static BookstoreManagerService _managerService = ServiceHelper.GetService<BookstoreManagerService>();

    public SectionByTeacherCollectionViewModel()
    {
        RegistrarService registrar = ServiceHelper.GetService<RegistrarService>();
        _managerService.OnCacheRefreshed += (_, _) =>
        {
            ViewModelCache = [];
            _ = GetClonedViewModels(registrar.TeacherList);
        };
    }

    [ObservableProperty] UserViewModel teacher = IEmptyViewModel<UserViewModel>.Empty;
    [ObservableProperty] ImmutableArray<SectionViewModel> sections = [];

    public static ConcurrentBag<SectionByTeacherCollectionViewModel> ViewModelCache { get; private set; } = [];

    public static SectionByTeacherCollectionViewModel Get(UserRecord model)
    {
        var vm = ViewModelCache.FirstOrDefault(col => col.Teacher.Id == model.id);
        if (vm is not null)
            return vm.Clone();

        vm = new()
        {
            Teacher = UserViewModel.Get(model),
            Sections = SectionViewModel.GetClonedViewModels(_managerService.SectionsByTeacher[model.id]).ToImmutableArray()
        };
        ViewModelCache.Add(vm);
        return vm.Clone();
    }

    public static List<SectionByTeacherCollectionViewModel> GetClonedViewModels(IEnumerable<UserRecord> models)
    {
        List<SectionByTeacherCollectionViewModel> result = [];
        foreach (var model in models)
            result.Add(Get(model));
        return result;
    }

    public static async Task Initialize(BookstoreManagerService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

    }

    public SectionByTeacherCollectionViewModel Clone() => (SectionByTeacherCollectionViewModel)MemberwiseClone();
}

public partial class SectionByDepartmentCollectionViewModel :
    ObservableObject,
    ICachedViewModel<SectionByDepartmentCollectionViewModel, string, BookstoreManagerService>
{
    private static readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();
    private static readonly BookstoreManagerService _managerService = ServiceHelper.GetService<BookstoreManagerService>();

    [ObservableProperty] string department = "";
    [ObservableProperty] ImmutableArray<SectionViewModel> sections = [];

    public static ConcurrentBag<SectionByDepartmentCollectionViewModel> ViewModelCache { get; private set; } = [];

    public static SectionByDepartmentCollectionViewModel Get(string dept)
    {
        var vm = ViewModelCache.FirstOrDefault(col => col.Department == dept);
        if (vm is not null)
            return vm.Clone();
        var courses = _registrar.GetDeptCourses(dept);

        vm = new()
        {
            Department = dept,
            Sections = SectionViewModel.GetClonedViewModels(
                _managerService
                    .ProtoSections
                    .Where(sec => courses.Any(c => c.courseId == sec.course.courseId))
                ).ToImmutableArray()
        };
        ViewModelCache.Add(vm);
        return vm.Clone();
    }

    public static List<SectionByDepartmentCollectionViewModel> GetClonedViewModels(IEnumerable<string> depts)
    {
        List<SectionByDepartmentCollectionViewModel> result = [];
        foreach (var dept in depts)
            result.Add(Get(dept));
        return result;
    } 

    public static async Task Initialize(BookstoreManagerService service, ErrorAction onError)
    {
        await _registrar.WaitForInit(onError);
        await service.WaitForInit(onError);

        _ = GetClonedViewModels(_registrar.DepartmentList);
    }

    public SectionByDepartmentCollectionViewModel Clone() => (SectionByDepartmentCollectionViewModel)MemberwiseClone();
}

public partial class SectionSearchViewModel :
    ObservableObject
{
    public class SectionSearchMode
    {
        public static readonly SectionSearchMode ByTeacher = new("By Teacher");
        public static readonly SectionSearchMode ByDepartment = new("By Department");
        public string Label { get; init; }

        private SectionSearchMode(string label) => Label = label;

        public override string ToString() => Label;

        public static implicit operator string(SectionSearchMode mode) => mode.Label;

        public override bool Equals(object? obj)

        {
            if (obj is SectionSearchMode mode)
                return mode.Label == this.Label;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Label);
        }
    }

    [ObservableProperty] ImmutableArray<SectionByDepartmentCollectionViewModel> departmentSearch = [];
    [ObservableProperty] ImmutableArray<SectionByTeacherCollectionViewModel> teacherSearch = [];
    [ObservableProperty] SectionSearchMode searchMode = SectionSearchMode.ByTeacher;
    [ObservableProperty] UserSearchViewModel userSearch = new();


    [RelayCommand]
}
