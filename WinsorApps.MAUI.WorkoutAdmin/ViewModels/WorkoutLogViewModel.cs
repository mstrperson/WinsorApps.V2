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
using WinsorApps.Services.Athletics.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.WorkoutAdmin.ViewModels;

public partial class WorkoutLogViewModel :
    ObservableObject,
    IModelCarrier<WorkoutLogViewModel, WorkoutLog>
{
    [ObservableProperty] DateTime startDate;
    [ObservableProperty] DateTime endDate;
    [ObservableProperty] ObservableCollection<WorkoutViewModel> allWorkouts = [];
    [ObservableProperty] LogFilter filter = new(ServiceHelper.GetService<RegistrarService>());
    [ObservableProperty] ObservableCollection<WorkoutViewModel> filteredWorkouts;


    public OptionalStruct<WorkoutLog> Model { get; private set; }

    public static WorkoutLogViewModel Get(WorkoutLog model)
    {
        var log = new WorkoutLogViewModel()
        {
            StartDate = model.dateRange.start.ToDateTime(default),
            EndDate = model.dateRange.end.ToDateTime(default),
            AllWorkouts = [.. model.workouts.Select(WorkoutViewModel.Get)],
            Model = OptionalStruct<WorkoutLog>.Some(model)
        };

        log.FilteredWorkouts = log.AllWorkouts;

        foreach (var workout in log.AllWorkouts)
        {
            workout.Invalidated += (_, _) =>
            {
                log.AllWorkouts.Remove(workout);
                log.ApplyFilter();
            };
        }
        return log;
    }

    [RelayCommand]
    public void ApplyFilter()
    {
        FilteredWorkouts = [.. AllWorkouts.Where(Filter.Matches)];
    }
}

public partial class LogFilter : ObservableObject
{
    public event EventHandler? ApplyRequested;

    public LogFilter(RegistrarService registrar)
    {
        registrar.WaitForInit(err => { })
            .WhenCompleted(() =>
            {
                UserSearch.SetAvailableUsers(registrar.AllUsers);
            });
    }

    [ObservableProperty] bool forCreditOnly;
    [ObservableProperty] bool byClass;
    [ObservableProperty] ObservableCollection<SelectableLabelViewModel> classes = [ "Class V", "Class VI", "Class VII", "Class VIII" ];
    [ObservableProperty] bool byUser;
    [ObservableProperty] UserSearchViewModel userSearch = new();

    internal bool Matches(WorkoutViewModel workout)
    {
        return (!ForCreditOnly || workout.ForCredit)
            && (!ByClass || Classes.Any(cn => cn.IsSelected && workout.Student.DisplayName.Contains(cn.Label)))
            && (!ByUser || (UserSearch.IsSelected && UserSearch.Selected.BlackbaudId == workout.Student.BlackbaudId));
    }

    [RelayCommand]
    public void Reset()
    {
        ForCreditOnly = false;
        ByClass = false;
        foreach (var item in Classes) 
            item.IsSelected = false;
        ByUser = false;
        UserSearch.ClearSelection();
    }

    [RelayCommand]
    public void ApplyFilter() => ApplyRequested?.Invoke(this, EventArgs.Empty);
}
