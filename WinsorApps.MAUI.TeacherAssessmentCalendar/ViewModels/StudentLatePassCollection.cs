using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class StudentLatePassCollectionViewModel(UserViewModel student) :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] UserViewModel student = student;
    [ObservableProperty] ObservableCollection<TeacherLatePassViewModel> latePasses = [];
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool showPast;

    [RelayCommand]
    public async Task ToggleShowPast()
    {
        ShowPast = !ShowPast;
        await LoadPasses();
    }

    [RelayCommand]
    public async Task LoadPasses()
    {
        var passes = await _service.GetStudentPassess(Student.Id, OnError.DefaultBehavior(this), ShowPast);
        LatePasses = [.. passes.Select(LatePassViewModel.Get)];
        foreach(var pass in LatePasses)
        {
            pass.Withdrawn += (_, _) => LatePasses.Remove(pass);
            pass.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }
    }
    
    [RelayCommand]
    public async Task RequestNewPassFor(AssessmentCalendarEvent assessment)
    {
        var pass = await _service.RequestPassForStudent(Student.Id, assessment.id, OnError.DefaultBehavior(this));
        if(pass is not null)
        {
            var detail = new AssessmentPassDetail(assessment, Student.Model.Reduce(UserRecord.Empty), pass.timeStamp, MakeupTime.Default);
            TeacherLatePassViewModel vm = LatePassViewModel.Get(detail);
            vm.Withdrawn += (_, _) => LatePasses.Remove(vm);
            LatePasses.Add(vm);
        }
    }
}

public partial class TeacherLatePassViewModel(LatePassViewModel latePass) :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherAssessmentService _service = ServiceHelper.GetService<TeacherAssessmentService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? Withdrawn;

    [ObservableProperty] LatePassViewModel latePass = latePass;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public static implicit operator TeacherLatePassViewModel(LatePassViewModel model) => new(model);

    [RelayCommand]
    public async Task WithdrawPass()
    {
        Busy = true;
        BusyMessage = $"Withdrawing late pass for {LatePass.CourseName} - {LatePass.Note} on behalf of {LatePass.Student.DisplayName}";
        var success = true;
        await _service.WithdrawLatePassForStudent(LatePass.Student.Id, LatePass.Model.Reduce(AssessmentPassDetail.Empty).assessment.id, err =>
        {
            success = false;
            OnError.DefaultBehavior(this)(err);
        });

        if(success)
            Withdrawn?.Invoke(this, EventArgs.Empty);

        Busy = false;
    }
}
