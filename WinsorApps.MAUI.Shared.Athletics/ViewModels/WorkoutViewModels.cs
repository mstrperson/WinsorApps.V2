using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Athletics.Models;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Athletics.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Immutable;

namespace WinsorApps.MAUI.Shared.Athletics.ViewModels;

public partial class WorkoutViewModel :
    ObservableObject,
    IModelCarrier<WorkoutViewModel, Workout>,
    IErrorHandling,
    IBusyViewModel
{
    private readonly WorkoutService _workoutService = ServiceHelper.GetService<WorkoutService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] UserViewModel student = UserViewModel.Empty;
    [ObservableProperty] DateTime timeIn;
    [ObservableProperty] DateTime timeOut;
    [ObservableProperty] bool isOpen = true;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool forCredit;

    public OptionalStruct<Workout> Model { get; private set; } = OptionalStruct<Workout>.None();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? SignedOut;
    public event EventHandler? Invalidated;

    private WorkoutViewModel() { }

    [RelayCommand]
    public async Task SignOut()
    {
        Busy = true;
        var time = DateTime.Now;
        var duration = time - TimeIn;
        BusyMessage = $"Signing Out {Student.DisplayName} at {time:hh:mm tt} [Workout Time: {duration.TotalMinutes:#} Minutes].";

        var result = await _workoutService.SignOut(Id, OnError.DefaultBehavior(this));
        if (result.HasValue)
        {
            Model = OptionalStruct<Workout>.Some(result.Value);
            TimeOut = result.Value.timeOut ?? default;
            IsOpen = !result.Value.timeOut.HasValue;
            if (!IsOpen)
                SignedOut?.Invoke(this, EventArgs.Empty);
        }

        Busy = false;
    }

    [RelayCommand]
    public async Task Invalidate()
    {
        Busy = true;
        var time = DateTime.Now;
        var duration = (time - TimeIn).TotalHours;
        BusyMessage = $"Invalidating {Student.DisplayName} at {time:hh:mm tt} [Workout Time: {duration:#.##}].";

        if (await _workoutService.InvalidateWorkout(Id, OnError.DefaultBehavior(this)))
            Invalidated?.Invoke(this, EventArgs.Empty);

        Busy = false;
    }

    public static WorkoutViewModel Get(Workout model)
    {
        var vm = new WorkoutViewModel()
        {
            Id = model.id,
            IsOpen = !model.timeOut.HasValue,
            TimeIn = model.timeIn,
            TimeOut = model.timeOut ?? default,
            Student = UserViewModel.Get(model.user),
            Model = OptionalStruct<Workout>.Some(model),
            ForCredit = model.workoutDetails.Any(tag => tag.Contains("credit", StringComparison.InvariantCultureIgnoreCase))
        };

        return vm;
    }
}

public partial class NewWorkoutViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly RegistrarService _registrar;
    private readonly WorkoutService _workoutService;

    public event EventHandler<WorkoutViewModel>? NewSignIn;

    public NewWorkoutViewModel(RegistrarService registrar, WorkoutService workoutService)
    {
        _registrar = registrar;
        _workoutService = workoutService;

        _registrar.WaitForUniqueNames()
            .WhenCompleted(() =>
            {
                StudentSearch.SetAvailableUsers(_registrar.AllUsers);
            });
        StudentSearch.OnError += (sender, err) => OnError?.Invoke(sender, err);
        StudentSearch.OnSingleResult += (_, student) =>
        {
            SelectedStudent = student;
            IsSelected = true;
            ShowForCredit = student.Model.Reduce(UserRecord.Empty).studentInfo.HasValue;
        };
    }

    [RelayCommand]
    public void Clear()
    {
        StudentSearch.ClearSelection();
        SelectedStudent = UserViewModel.Empty;
        IsSelected = false;
        ShowForCredit = false;
        ForCredit = false;
    }

    [RelayCommand]
    public async Task SignIn()
    {
        if (!IsSelected) return;

        Busy = true;
        BusyMessage = $"Signing In {SelectedStudent.DisplayName} at {DateTime.Now:hh:mm tt}";
        ImmutableArray<string> tags = ForCredit ? [ "Credit" ] : [];
        var result = await _workoutService.SignIn(SelectedStudent.Id, tags, OnError.DefaultBehavior(this));
        if (result.HasValue)
        {
            
            NewSignIn?.Invoke(this, WorkoutViewModel.Get(result.Value));
            Clear();
        }

        Busy = false;
    }

    [RelayCommand]
    public void ToggleForCredit()
    {
        ForCredit = !ForCredit;
    }

    [ObservableProperty] UserSearchViewModel studentSearch = new();
    [ObservableProperty] UserViewModel selectedStudent = UserViewModel.Empty;
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool forCredit;
    [ObservableProperty] bool showForCredit;

    public event EventHandler<ErrorRecord>? OnError;
}

