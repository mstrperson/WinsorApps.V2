using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class SectionViewModel : ObservableObject
{
    private readonly SectionRecord _section;

    public event EventHandler<UserRecord>? TeacherSelected;
    public event EventHandler<UserRecord>? StudentSelected;

    public event EventHandler<SectionRecord>? Selected;
    
    public string DisplayName => _section.displayName;

    [ObservableProperty] private ImmutableArray<UserViewModel> teachers;

    [ObservableProperty] private ImmutableArray<UserViewModel> students;

    public SectionViewModel(SectionRecord section)
    {
        _section = section;
        // Get the RegistrarService from the service helper...
        var registrar = ServiceHelper.GetService<RegistrarService>()!;
        
        // Get data about the teachers of this section
        // and create UserViewModels for each of them
        teachers = registrar.TeacherList
            .Where(t => _section.teachers.Any(tch => t.id == tch.id))
            .Select(t => new UserViewModel(t))
            .ToImmutableArray();
        
        // 
        foreach (var teacher in Teachers)
            teacher.Selected += (sender, tch) => TeacherSelected?.Invoke(sender, tch);
        
        students = registrar.StudentList
            .Where(s => _section.students.Any(stu => stu.id == s.id))
            .Select(s => new UserViewModel(s))
            .ToImmutableArray();
        
        foreach (var student in Students)
            student.Selected += (sender, stu) => StudentSelected?.Invoke(sender, stu);
    }

    [RelayCommand]
    public void Select()
    {
        Selected?.Invoke(this, _section);
    }
}