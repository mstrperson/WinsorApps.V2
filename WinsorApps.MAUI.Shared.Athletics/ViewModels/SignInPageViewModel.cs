
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Athletics.Services;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.Athletics.ViewModels;

public partial class SignInPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly WorkoutService _service;
    private readonly RegistrarService _registrar;

    [ObservableProperty] NewWorkoutViewModel newSignIn;
    [ObservableProperty] ObservableCollection<WorkoutViewModel> openWorkouts = [];
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] bool showNewSignin;

    public SignInPageViewModel(NewWorkoutViewModel newSignIn, WorkoutService service, RegistrarService registrar)
    {
        this.newSignIn = newSignIn;
        _service = service;
        _registrar = registrar;
        Initailize().SafeFireAndForget(e => e.LogException());

        NewSignIn.NewSignIn += (_, workout) =>
        {
            OpenWorkouts.Add(workout);
            workout.OnError += (sender, e) => OnError?.Invoke(sender, e);
            workout.Invalidated += (_, _) => OpenWorkouts.Remove(workout);
            workout.SignedOut += (_, _) => OpenWorkouts.Remove(workout);
            workout.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            NewSignIn.Clear();
            ShowNewSignin = false;
        };
    }
    private async Task RefreshInBackground(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5));
            await Refresh();
        }
    }

    private async Task Initailize()
    {
        Busy = true;
        BusyMessage = "Initializing Application";
        while (!_service.Ready)
            await Task.Delay(500);

        if (!_registrar.Started)
            await _registrar.Initialize(OnError.DefaultBehavior(this));
        else
            await _registrar.WaitForUniqueNames();

        OpenWorkouts = [.. _service.OpenWorkouts.Select(WorkoutViewModel.Get)];
        foreach (var workout in OpenWorkouts)
        {
            workout.OnError += (sender, e) => OnError?.Invoke(sender, e);
            workout.Invalidated += (_, _) => OpenWorkouts.Remove(workout);
            workout.SignedOut += (_, _) => OpenWorkouts.Remove(workout);
            workout.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }
        Busy = false;

        RefreshInBackground(CancellationToken.None).SafeFireAndForget(e => e.LogException());
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
            workout.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }

        Busy = false;
    }

    [RelayCommand]
    public void ToggleShowNewSignin()
    {
        ShowNewSignin = !ShowNewSignin;
        if (!ShowNewSignin)
        {
            NewSignIn.Clear();
        }
    }

    public event EventHandler<ErrorRecord>? OnError;
}
