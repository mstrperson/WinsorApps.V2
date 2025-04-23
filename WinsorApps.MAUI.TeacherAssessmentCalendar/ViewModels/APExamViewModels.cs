using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class APExamDetailViewModel :
    ObservableObject,
    IErrorHandling,
    ISelectable<APExamDetailViewModel>,
    IModelCarrier<APExamDetailViewModel, APExamDetail>,
    IBusyViewModel,
{

    [ObservableProperty] bool isSelected;
    public Optional<APExamDetail> Model { get; private set; } = Optional<APExamDetail>.None();

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Loading...";

    [ObservableProperty] string id = "";
    [ObservableProperty] string courseName = "";
    [ObservableProperty] DateTime startDateTime = DateTime.Now;
    [ObservableProperty] DateTime endDateTime = DateTime.Now;
    [ObservableProperty] ObservableCollection<SectionViewModel> sections = [];
    [ObservableProperty] ObservableCollection<StudentViewModel> students = [];


    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<APExamDetailViewModel>? Selected;

    public static APExamDetailViewModel Get(APExamDetail model)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();

        var vm = new APExamDetailViewModel
        {
            Id = model.id,
            CourseName = model.courseName,
            StartDateTime = model.startDateTime,
            EndDateTime = model.endDateTime,
            Sections = [ .. model.sectionIds
                                .Select(id => registrar.SectionDetailCache.ContainsKey(id) ? registrar.SectionDetailCache[id] : null)
                                .Where(sec => sec is not null)
                                .Select(sec => SectionViewModel.Get(sec!))],
            Students = [.. model.studentIds
                                .Select(id => registrar.StudentList.FirstOrDefault(stu => stu.id == id))
                                .Where(stu => stu is not null)
                                .Select(stu => StudentViewModel.Get(stu!))],
            Model = Optional<APExamDetail>.Some(model)
        };

        return vm;
    }

    public void Select()
    {
        throw new NotImplementedException();
    }
}
