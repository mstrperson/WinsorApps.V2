using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Athletics.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Athletics.Services;

public class WorkoutService :
    IAsyncInitService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public ImmutableArray<Workout> OpenWorkouts { get; private set; } = [];

    public WorkoutService(ApiService api, LocalLoggingService logging)
    {
        _api = api;
        _logging = logging;
    }

    public string CacheFileName => $"{_logging.AppStoragePath}{Path.DirectorySeparatorChar}.workout.cache";

    public bool Started { get; private set; }

    public bool Ready { get; private set; }

    public double Progress { get; private set; }

    public async Task Initialize(ErrorAction onError)
    {
        while (!_api.Ready)
            await Task.Delay(250);

        Started = true;

        OpenWorkouts = await GetOpenWorkouts(onError);
        Progress = 1;
        Ready = true;
    }

    public async Task<ImmutableArray<Workout>> GetOpenWorkouts(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<Workout>?>(HttpMethod.Get, "api/athletics/open-workouts", onError: onError);
        if (!result.HasValue)
            return [];

        return result.Value;
    }

    public async Task<Workout?> SignIn(string studentId, ErrorAction onError)
    {
        var result = await _api.SendAsync<Workout?>(HttpMethod.Post, $"api/athletics/{studentId}/sign-in", onError: onError);
        if (result.HasValue)
            OpenWorkouts = [.. OpenWorkouts, result.Value];

        return result;
    }

    public async Task<Workout?> SignOut(string workoutId, ErrorAction onError)
    {
        var result = await _api.SendAsync<Workout?>(HttpMethod.Post, $"api/athletics/{workoutId}/sign-out", onError: onError);
        if (result.HasValue)
        {
            var exisiting = OpenWorkouts.FirstOrDefault(wk => wk.id == workoutId);
            if (exisiting == default)
                OpenWorkouts = [.. OpenWorkouts, result.Value];
            else
                OpenWorkouts = OpenWorkouts.Replace(exisiting, result.Value);
        }

        return result;
    }

    public async Task<bool> InvalidateWorkout(string workoutId, ErrorAction onError)
    {
        var success = true;
        await _api.SendAsync(HttpMethod.Delete, $"api/athletics/{workoutId}/invalidate", onError: err =>
        {
            success = false;
            onError(err);
        });

        if (success)
        {
            var workout = OpenWorkouts.FirstOrDefault(wk => wk.id == workoutId);
            if (workout != default)
                OpenWorkouts = OpenWorkouts.Remove(workout);
        }

        return success;
    }

    public bool LoadCache()
    {
        return true;
    }

    public async Task Refresh(ErrorAction onError)
    {
        await Initialize(onError);
    }

    public void SaveCache()
    {
        // Don't Cache this service....
    }

    public async Task WaitForInit(Action<ErrorRecord> onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }
}

