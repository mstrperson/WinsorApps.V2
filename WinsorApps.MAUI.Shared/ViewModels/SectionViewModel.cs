using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class SectionViewModel : ObservableObject, IEmptyViewModel<SectionViewModel>, ISelectable<SectionViewModel>
{
    public readonly SectionRecord Section;

    public event EventHandler<UserViewModel>? TeacherSelected;
    public event EventHandler<UserViewModel>? StudentSelected;

    public event EventHandler<SectionViewModel>? Selected;
    
    public string DisplayName => Section.displayName;

    [ObservableProperty] private ImmutableArray<UserViewModel> teachers = [];

    [ObservableProperty] private ImmutableArray<UserViewModel> students = [];
    [ObservableProperty] bool isSelected;

    public SectionViewModel()
    {
        Section = new();
    }

    public SectionViewModel(SectionRecord section)
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
        Selected?.Invoke(this, this);
    }
}