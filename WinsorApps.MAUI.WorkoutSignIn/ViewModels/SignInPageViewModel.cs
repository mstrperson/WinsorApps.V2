using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Athletics.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Athletics.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.WorkoutSignIn.ViewModels;

public partial class SignInPageViewModel : 
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly WorkoutService _service;

    [ObservableProperty] NewWorkoutViewModel newSignIn;
    [ObservableProperty] ObservableCollection<WorkoutViewModel> openWorkouts = [];
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public SignInPageViewModel(NewWorkoutViewModel newSignIn, WorkoutService service)
    {
        this.newSignIn = newSignIn;
        _service = service;
        _service.WaitForInit(OnError.DefaultBehavior(this))
            .WhenCompleted(() =>
            {
                OpenWorkouts = [.. _service.OpenWorkouts.Select(WorkoutViewModel.Get)];
                foreach (var workout in OpenWorkouts)
                {
                    workout.OnError += (sender, e) => OnError?.Invoke(sender, e);
                    workout.Invalidated += (_, _) => OpenWorkouts.Remove(workout);
                    workout.SignedOut += (_, _) => OpenWorkouts.Remove(workout);
                }
            });

        NewSignIn.NewSignIn += (_, workout) =>
        {
            OpenWorkouts.Add(workout);
            workout.OnError += (sender, e) => OnError?.Invoke(sender, e);
            workout.Invalidated += (_, _) => OpenWorkouts.Remove(workout);
            workout.SignedOut += (_, _) => OpenWorkouts.Remove(workout);
        };
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Busy = true;
        BusyMessage = "Refreshing Workouts";
        await _service.Refresh(OnError.DefaultBehavior(this));
        OpenWorkouts = [.. _service.OpenWorkouts.Select(WorkoutViewModel.Get)];
        foreach (var workout in OpenWorkouts)
        {
            workout.OnError += (sender, e) => OnError?.Invoke(sender, e);
            workout.Invalidated += (_, _) => OpenWorkouts.Remove(workout);
            workout.SignedOut += (_, _) => OpenWorkouts.Remove(workout);
        }
    }

    public event EventHandler<ErrorRecord>? OnError;
}
