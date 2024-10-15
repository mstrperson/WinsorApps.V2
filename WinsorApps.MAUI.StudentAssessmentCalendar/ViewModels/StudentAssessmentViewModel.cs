using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;

public partial class StudentAssessmentViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    ISelectable<StudentAssessmentViewModel>
{
    private readonly StudentAssessmentService _service = ServiceHelper.GetService<StudentAssessmentService>();
    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentAssessmentViewModel>? Selected;
    public event EventHandler<StudentAssessmentViewModel>? LatePassRequested;
    public event EventHandler<StudentAssessmentViewModel>? LatePassSubmitted;

    [ObservableProperty] AssessmentCalendarEventViewModel @event;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] string className = "";
    [ObservableProperty] string teacherName = "";
    [ObservableProperty] bool cannotLatePass;
    [ObservableProperty] string latePassMessage = "";
    [ObservableProperty] bool isAssessment;
    [ObservableProperty] bool isSelected;

    [ObservableProperty] LatePassViewModel latePass;

    public static implicit operator StudentAssessmentViewModel(AssessmentCalendarEventViewModel evt) => new(evt);

    public StudentAssessmentViewModel(AssessmentCalendarEventViewModel @event)
    {
        if (string.IsNullOrEmpty(@event.Model.Reduce(AssessmentCalendarEvent.Empty).id))
        {
            Event = @event;
            return;
        }
        this.@event = AssessmentCalendarEventViewModel.Get(_service.MyCalendar.First(evt => evt.type == @event.Type && evt.id == @event.Id));
        if(@event.Type != AssessmentType.Note)
            LoadDetails().SafeFireAndForget(e => e.LogException());
        else
        {
            ClassName = @event.Summary;
        }

        latePass = LatePassViewModel.CreateEmpty(UserViewModel.Get(_registrar.Me), @event);
        

        _service.OnCacheRefreshed += (_, _) =>
        {
            Event = AssessmentCalendarEventViewModel.Get(_service.MyCalendar.First(evt => evt.type == @event.Type && evt.id == @event.Id)); 
            if (@event.Type != AssessmentType.Note)
                LoadDetails().SafeFireAndForget(e => e.LogException());
        };
    }

    [RelayCommand]
    public async Task LoadDetails()
    {
        Busy = true;
        BusyMessage = "Getting Details...";
        if (Event.Type == AssessmentType.Assessment)
        {
            var result = await _service.GetAssessmentDetails(Event.Id, OnError.DefaultBehavior(this));
            if (result.HasValue)
            {
                var Model = result.Value;
                ClassName = $"{Model.displayName} [{Model.block}]";
                TeacherName = $"{Model.teacher.firstName} {Model.teacher.lastName}";
                CannotLatePass = (!Event.PassAvailable && !Event.PassUsed) || Event.Start < DateTime.Now;
                LatePassMessage = "You've already used your Late Pass for this Semester";
                if (Event.PassUsed && Event.Start < DateTime.Now)
                    LatePassMessage = "You cannot withdraw your late pass after the assessment has started.";
                if (Event.PassAvailable)
                    LatePassMessage = $"You may still submit a late pass for this assessment.{Environment.NewLine}But you will not be able to withdraw it afterward.";
                IsAssessment = true;
            }
        }
        if(Event.Type == AssessmentType.ApExam)
        {
            var result = await _service.GetApExamDetails(Event.Id, OnError.DefaultBehavior(this));
            if (result.HasValue)
            {
                var Model = result.Value;

                ClassName = $"{Model.courseName}";
            }
        }
        Busy = false;
    }

    [RelayCommand]
    public void RequestLatePass() => LatePassRequested?.Invoke(this, this);

    [RelayCommand]
    public async Task SubmitLatePass()
    {
        Busy = true;
        BusyMessage = "Requesting Late Pass";
        var result = await _service.RequestLatePass(Event.Id, OnError.DefaultBehavior(this), LatePass.MakeupTime);
        if(result.HasValue)
        {
            Event.PassAvailable = false;
            Event.PassUsed = true;
            LatePassSubmitted?.Invoke(this, this);
        }
        Busy = false;
    }

    [RelayCommand]
    public async Task WithdrawLatePass()
    {
        Busy = true;
        BusyMessage = "Withdrawing Late Pass";
        var result = await _service.WithdrawLatePass(Event.Id, OnError.DefaultBehavior(this));
        if (result)
        {
            Event.PassAvailable = true;
            Event.PassUsed = false;
        }
        Busy = false;
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class LatePassCollectionViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly StudentAssessmentService _service;
    private readonly RegistrarService _registrar;

    public LatePassCollectionViewModel(StudentAssessmentService service, RegistrarService registrar)
    {
        _service = service;
        _registrar = registrar;
        LoadData();

        _service.OnCacheRefreshed += (_, args) =>
        {
            if (args is StudentAssessmentCacheRefreshedEventArgs sacrea && sacrea.Passes)
            {
                LoadData();
            }
        };
    }

    private void LoadData()
    {
        MyLatePasses = [.. _service.MyLatePasses.Select(LatePassViewModel.Get)];
        AvailablePasses = [..
            _registrar
                .MyAcademicSchedule
                .Where(section => !_service.MyLatePasses.Any(pass => _registrar.SectionDetailCache[section.sectionId].course.courseCode == pass.assessment.summary))
                .Select(SectionViewModel.Get)];

        foreach(var lp in MyLatePasses)
        {
            lp.LoadAssessmentRequested += async (_, detail) =>
            {
                _ = await _service.GetMyCalendarOn(lp.DateAndTime.Date, OnError.DefaultBehavior(this));
                var asmt = _service.MyCalendar.First(evt => evt.type == AssessmentType.Assessment && evt.id == detail.assessment.id);
                LoadAssessmentRequested?.Invoke(this, AssessmentCalendarEventViewModel.Get(asmt));
            };
        }
    }

    [ObservableProperty] ObservableCollection<LatePassViewModel> myLatePasses = [];
    [ObservableProperty] ObservableCollection<SectionViewModel> availablePasses = [];
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "Loading...";

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<StudentAssessmentViewModel>? LoadAssessmentRequested;

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        _ = await _service.GetMyPasses(OnError.DefaultBehavior(this));
        LoadData();
        Busy = false;
    }
}