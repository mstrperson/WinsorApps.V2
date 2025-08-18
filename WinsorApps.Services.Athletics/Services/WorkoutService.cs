using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Athletics.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Athletics.Services;

public class WorkoutService(ApiService api, LocalLoggingService logging) :
    IAsyncInitService
{
    private readonly ApiService _api = api;
    private readonly LocalLoggingService _logging = logging;

    public List<string> Tags { get; private set; } = [];

    public List<Workout> OpenWorkouts { get; private set; } = [];

    public string CacheFileName => $"{_logging.AppStoragePath}{Path.DirectorySeparatorChar}.workout.cache";
    public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }

    public bool Started { get; private set; }

    public bool Ready { get; private set; }

    public double Progress { get; private set; }

    public async Task Initialize(ErrorAction onError)
    {
        while (!_api.Ready)
            await Task.Delay(250);

        Started = true;

        OpenWorkouts = await GetOpenWorkouts(onError);

        Tags = (await _api.SendAsync<List<string>>(HttpMethod.Get, "api/athletics/tags/list", onError: onError)) ?? [];

        Progress = 1;
        Ready = true;
    }

    public async Task<List<Workout>> GetOpenWorkouts(ErrorAction onError)
    {
        var result = await _api.SendAsync<List<Workout>>(HttpMethod.Get, "api/athletics/open-workouts", onError: onError);
        if (result is null)
            return [];

        return [.. result.Where(wk => !wk.invalidated)];
    }

    public async Task<Workout?> SignIn(string studentId, List<string> tags, ErrorAction onError)
    {
        var result = await _api.SendAsync<List<string>,Workout?>(HttpMethod.Post, $"api/athletics/{studentId}/sign-in", tags, onError: onError);
        if (result is not null)
            OpenWorkouts = [.. OpenWorkouts, result];

        return result;
    }

    public async Task<Workout?> SignOut(string workoutId, ErrorAction onError)
    {
        var result = await _api.SendAsync<Workout?>(HttpMethod.Post, $"api/athletics/{workoutId}/sign-out", onError: onError);
        if (result is not null)
        {
            var exisiting = OpenWorkouts.FirstOrDefault(wk => wk.id == workoutId);
            if (exisiting == default)
                OpenWorkouts = [.. OpenWorkouts, result];
            else
                OpenWorkouts.Replace(exisiting, result);
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
                OpenWorkouts.Remove(workout);
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
        var result = await _api.SendAsync<WorkoutLog>(HttpMethod.Get, $"api/athletics/logs?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}", onError: onError);

        return result ?? new([], new(start, end));
    }

    public async Task<WorkoutLog> GetForCreditWorkouts(DateOnly start, DateOnly end, ErrorAction onError)
    {
        var result = await _api.SendAsync<WorkoutLog>(HttpMethod.Get, $"api/athletics/logs?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}&forCreditOnly=true", onError: onError);

        return result ?? new([], new(start, end));
    }

    public async Task<Workout?> CreateOrUpdateWorkout(Workout workout, ErrorAction onError)
    {
        var result = await _api.SendAsync<Workout, Workout?>(HttpMethod.Put, "api/athletics/direct-edit", workout, onError: onError);
        if (result is not null)
        {
            workout = result;
               
            OpenWorkouts = [.. OpenWorkouts.Except(OpenWorkouts.Where(wk => wk.id == workout.id)), workout];
        }

        return result;
    }
}

