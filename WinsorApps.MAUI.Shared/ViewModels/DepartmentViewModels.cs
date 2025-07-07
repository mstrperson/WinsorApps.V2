using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class DepartmentViewModel :
    ObservableObject,
    IErrorHandling,
    ISelectable<DepartmentViewModel>
{
    private static readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<DepartmentViewModel>? Selected;

    [ObservableProperty] private ObservableCollection<CourseViewModel> courses = [];
    [ObservableProperty] private string name = "";

    public static readonly DepartmentViewModel Empty = new();

    [ObservableProperty] private bool isSelected;

    public static DepartmentViewModel Get(string departmentName) =>
        new()
        {
            Name = departmentName,
            Courses = 
            [
                .. _registrar.CourseList
                    .Where(course => course.department.Equals(departmentName, StringComparison.InvariantCultureIgnoreCase))
                    .Select(CourseViewModel.Get)
            ]
        };

    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class DepartmentSearchViewModel(RegistrarService registrar) :
    ObservableObject,
    IErrorHandling
{
    private readonly RegistrarService _registrar = registrar;
    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] private ObservableCollection<DepartmentViewModel> departments = 
    [
        ..registrar.DepartmentList
            .Select(DepartmentViewModel.Get)
    ];

}
