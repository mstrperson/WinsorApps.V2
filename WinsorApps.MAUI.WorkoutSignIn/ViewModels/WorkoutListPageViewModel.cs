using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.MAUI.Shared.WorkoutSignin.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.WorkoutSignIn.Services;

namespace WinsorApps.MAUI.WorkoutSignIn.ViewModels;

public partial class WorkoutListPageViewModel :
    ObservableObject,
    IErrorHandling
{
    private readonly WorkoutService _service;

    [ObservableProperty] ObservableCollection<WorkoutViewModel> openWorkouts = [];
    [ObservableProperty] NewWorkoutViewModel signInViewModel;
    [ObservableProperty] bool showSignIn;

    [RelayCommand]
    public void ToggleShowSignIn() => ShowSignIn = !ShowSignIn;

    public WorkoutListPageViewModel(NewWorkoutViewModel signInViewModel, WorkoutService service)
    {
        _service = service;

        service.WaitForInit(OnError.DefaultBehavior(this))
            .WhenCompleted(() =>
            {
                OpenWorkouts = [.. _service.OpenWorkouts.Select(WorkoutViewModel.Get)];

                foreach(var workout in OpenWorkouts)
                {
                    workout.OnError += (sender, err) => OnError?.Invoke(sender, err);
                    workout.SignedOut += (_, _) => OpenWorkouts.Remove(workout);
                    workout.Invalidated += (_, _) => OpenWorkouts.Remove(workout);
                }
            });

        this.signInViewModel = signInViewModel;
        SignInViewModel.OnError += (sender, err) => OnError?.Invoke(sender, err);
        SignInViewModel.NewSignIn += (_, workout) =>
        {
            workout.OnError += (sender, err) => OnError?.Invoke(sender, err);
            workout.SignedOut += (_, _) => OpenWorkouts.Remove(workout);
            workout.Invalidated += (_, _) => OpenWorkouts.Remove(workout);
            OpenWorkouts.Add(workout);
        };
    }

    public event EventHandler<ErrorRecord>? OnError;
}
