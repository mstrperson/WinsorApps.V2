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

    public ImmutableArray<string> Tags { get; private set; } = [];

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

        Tags = await _api.SendAsync<ImmutableArray<string>>(HttpMethod.Get, "api/athletics/tags/list", onError: onError);

        Progress = 1;
        Ready = true;
    }

    public async Task<ImmutableArray<Workout>> GetOpenWorkouts(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<Workout>?>(HttpMethod.Get, "api/athletics/open-workouts", onError: onError);
        if (!result.HasValue)
            return [];

        return [.. result.Value.Where(wk => !wk.invalidated)];
    }

    public async Task<Workout?> SignIn(string studentId, ImmutableArray<string> tags, ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<string>,Workout?>(HttpMethod.Post, $"api/athletics/{studentId}/sign-in", tags, onError: onError);
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

    public async Task SaveCache() => await Task.CompletedTask;
    
    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }

    public async Task<WorkoutLog> GetAllWorkoutLogs(DateOnly start, DateOnly end, ErrorAction onError)
    {
        var result = await _api.SendAsync<WorkoutLog?>(HttpMethod.Get, $"api/athletics/logs?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}", onError: onError);

        return result ?? new([], new(start, end));
    }

    public async Task<WorkoutLog> GetForCreditWorkouts(DateOnly start, DateOnly end, ErrorAction onError)
    {
        var result = await _api.SendAsync<WorkoutLog?>(HttpMethod.Get, $"api/athletics/logs?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}&forCreditOnly=true", onError: onError);

        return result ?? new([], new(start, end));
    }

    public async Task<Workout?> CreateOrUpdateWorkout(Workout workout, ErrorAction onError)
    {
        var result = await _api.SendAsync<Workout, Workout?>(HttpMethod.Put, "api/athletics/direct-edit", workout, onError: onError);
        if (result.HasValue)
        {
            workout = result.Value;
               
            OpenWorkouts = [.. OpenWorkouts.Except(OpenWorkouts.Where(wk => wk.id == workout.id)), workout];
        }

        return result;
    }
}

