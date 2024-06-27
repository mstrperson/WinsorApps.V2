using System.Collections.Concurrent;
using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class CourseViewModel :
    ObservableObject,
    IDefaultValueViewModel<CourseViewModel>,
    ISelectable<CourseViewModel>,
    ICachedViewModel<CourseViewModel, CourseRecord, RegistrarService>
{
    private static readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    private static readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] string displayName = "";
    [ObservableProperty] string department = "";
    [ObservableProperty] string id = "";
    [ObservableProperty] int lengthInTerms;
    [ObservableProperty] string courseCode = "";
    [ObservableProperty] ImmutableArray<SectionViewModel> sections = [];

    public CourseRecord Course { get; init; } = default;

    public static ConcurrentBag<CourseViewModel> ViewModelCache { get; protected set; } = [];

    public static CourseViewModel Default => new();

    [ObservableProperty]
    public bool isSelected;

    public event EventHandler<CourseViewModel>? Selected;

    [RelayCommand]
    public async Task LoadSections()
    {
        bool success = true;
        var sections = await _registrar.GetSectionsOfAsync(Course, err =>
        {
            _logging.LogError(err);
            success = false;
        });
        if (!success)
            return;

        Sections = SectionViewModel.GetClonedViewModels(sections.Select(sec => 
            new SectionRecord(
                sec.sectionId, 
                sec.course.courseId, 
                sec.primaryTeacher.id,
                sec.teachers,
                sec.students,
                sec.term.termId,
                sec.room?.name ?? "",
                sec.block?.name ?? "",
                sec.displayName))).ToImmutableArray();
    }

    public static CourseViewModel Get(CourseRecord model)
    {
        var vm = ViewModelCache.FirstOrDefault(course => course.Id == model.courseId);
        if (vm is not null)
            return vm.Clone();

        vm = new()
        {
            Id = model.courseId,
            Department = model.department,
            LengthInTerms = model.lengthInTerms,
            CourseCode = model.courseCode,
            DisplayName = model.displayName,
            Course = model
        };
        ViewModelCache.Add(vm);
        return vm.Clone();
    }

    public static List<CourseViewModel> GetClonedViewModels(IEnumerable<CourseRecord> models)
    {
        List<CourseViewModel> result = [];
        foreach (var model in models)
            result.Add(Get(model));
        return result;
    }

    public static async Task Initialize(RegistrarService service, ErrorAction onError)
    {
        await service.WaitForInit(onError);

        _ = GetClonedViewModels(service.CourseList);
    }

    public CourseViewModel Clone() => new()
    {
        Id = Id,
        CourseCode = CourseCode,
        Department = Department,
        DisplayName = DisplayName,
        IsSelected = false,
        Course = Course,
        LengthInTerms = LengthInTerms,
        Sections = []
    };

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
            Selected?.Invoke(this, this);
    }
}

public partial class CourseListViewModel :
    ObservableObject,
    ICachedSearchViewModel<CourseViewModel>,
    IDefaultValueViewModel<CourseListViewModel>,
    IErrorHandling
{

    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    [ObservableProperty]
    private ImmutableArray<CourseViewModel> available = [];

    [ObservableProperty]
    private ImmutableArray<CourseViewModel> allSelected = [];

    [ObservableProperty]
    private ImmutableArray<CourseViewModel> options = [];

    [ObservableProperty]
    private CourseViewModel selected = CourseViewModel.Default;
    [ObservableProperty]
    private SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty]
    private string searchText = "";
    [ObservableProperty]
    private bool isSelected;
    [ObservableProperty]
    private bool showOptions;

    public static CourseListViewModel Default => CourseListViewModel.Default;

    public event EventHandler<ImmutableArray<CourseViewModel>>? OnMultipleResult;
    public event EventHandler<CourseViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults;
    public event EventHandler<ErrorRecord>? OnError;

    public CourseListViewModel()
    {
        _registrar.WaitForInit(OnError.DefaultBehavior(this)).WhenCompleted(() =>
        {
            Available = CourseViewModel.GetClonedViewModels(_registrar.CourseList).ToImmutableArray();
        });
    }

    [RelayCommand]
    public void Search()
    {
    }

    public void Select(CourseViewModel item)
    {
    }

    async Task IAsyncSearchViewModel<CourseViewModel>.Search() => await Task.Run(Search);
}


public partial class SectionViewModel : 
    ObservableObject,
    IDefaultValueViewModel<SectionViewModel>,
    ISelectable<SectionViewModel>,
    ICachedViewModel<SectionViewModel, SectionRecord, RegistrarService>
{
    public readonly SectionRecord Section;

    public event EventHandler<UserViewModel>? TeacherSelected;
    public event EventHandler<UserViewModel>? StudentSelected;

    public event EventHandler<SectionViewModel>? Selected;
    
    public string DisplayName => Section.displayName;

    public static ConcurrentBag<SectionViewModel> ViewModelCache { get; private set; } = [];

    public static SectionViewModel Default => new();

    [ObservableProperty] private ImmutableArray<UserViewModel> teachers = [];

    [ObservableProperty] private ImmutableArray<UserViewModel> students = [];
    [ObservableProperty] bool isSelected;

    public SectionViewModel()
    {
        Section = new();
    }

    private SectionViewModel(SectionRecord section)
    {
        Section = section;
        // Get the RegistrarService from the service helper...
        var registrar = ServiceHelper.GetService<RegistrarService>()!;
        
        // Get data about the teachers of this section
        // and create UserViewModels for each of them
        teachers = UserViewModel
            .GetClonedViewModels(
                registrar.TeacherList
                .Where(t => Section.teachers.Any(tch => t.id == tch.id)))
            .ToImmutableArray();
        
        // 
        foreach (var teacher in Teachers)
            teacher.Selected += (sender, tch) => TeacherSelected?.Invoke(sender, tch);
        
        students = UserViewModel
            .GetClonedViewModels(
                registrar.StudentList
                .Where(s => Section.students.Any(stu => stu.id == s.id)))
            .ToImmutableArray();
        
        foreach (var student in Students)
            student.Selected += (sender, stu) => StudentSelected?.Invoke(sender, stu);
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        if(IsSelected)
            Selected?.Invoke(this, this);
    }

    public static List<SectionViewModel> GetClonedViewModels(IEnumerable<SectionRecord> models)
    {
        List<SectionViewModel> result = [];
        foreach (var model in models)
            result.Add(Get(model));
        return result;
    }

    public static async Task Initialize(RegistrarService service, ErrorAction onError)
    {
        await Task.CompletedTask; // Cache to be built on demand.
    }

    public static SectionViewModel Get(SectionRecord model)
    {
        var vm = ViewModelCache.FirstOrDefault(sec => sec.Section.sectionId == model.sectionId);
        if (vm is not null)
            return vm.Clone();

        vm = new(model);
        ViewModelCache.Add(vm);
        return vm.Clone();
    }

    public SectionViewModel Clone() => (SectionViewModel)MemberwiseClone();
}