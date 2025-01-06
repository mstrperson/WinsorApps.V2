
using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Csv;
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
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.WorkoutAdmin.ViewModels;

public partial class WorkoutLogViewModel :
    ObservableObject,
    IModelCarrier<WorkoutLogViewModel, WorkoutLog>,
    IErrorHandling,
    IBusyViewModel
{
    [ObservableProperty] DateTime startDate;
    [ObservableProperty] DateTime endDate;
    [ObservableProperty] ObservableCollection<WorkoutViewModel> allWorkouts = [];
    [ObservableProperty] LogFilter filter = new(ServiceHelper.GetService<RegistrarService>());
    [ObservableProperty] ObservableCollection<WorkoutViewModel> filteredWorkouts;
    [ObservableProperty] bool displayFilter;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;

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

        log.Filter.ApplyRequested += (_, _) => log.ApplyFilter();

        foreach (var workout in log.AllWorkouts)
        {
            workout.OnError += (sender, err) => log.OnError?.Invoke(sender, err);
            workout.PropertyChanged += ((IBusyViewModel)log).BusyChangedCascade;
            workout.Invalidated += (_, _) =>
            {
                log.AllWorkouts.Remove(workout);
                log.ApplyFilter();
            };
        }
        return log;
    }

    [RelayCommand]
    public void ToggleDisplayFilter()
    {
        DisplayFilter = !DisplayFilter;
        if (!DisplayFilter)
        {
            Filter.Reset();
        }
    }

    [RelayCommand]
    public void ApplyFilter()
    {
        FilteredWorkouts = [.. AllWorkouts.Where(Filter.Matches)];
    }

    [RelayCommand]
    public async Task SaveVisibleWorkouts()
    {
        var output = new CSV();
        foreach(var workout in FilteredWorkouts)
        {
            var row = workout.Model.MapObject(model => new Row()
            {
                { "Last Name", model.user.lastName },
                { "First Name", model.user.firstName },
                { "Preferred Name", model.user.nickname },
                { "Class", model.user.studentInfo?.className ?? "" },
                { "Date", $"{model.timeIn:yyyy-MM-dd}" },
                { "Time In", $"{model.timeIn:hh:mm tt}" },
                { "Time Out", model.timeOut.HasValue ? $"{model.timeOut.Value:hh:mm tt}" : "Not Signed Out" },
                { "For Credit", workout.ForCredit ? "X" : "" }
            }, []);

            output.Add(row);
        }

        using MemoryStream ms = new();
        output.Save(ms);
        var data = ms.ToArray();

        using var saveStream = new MemoryStream(data);
        _ = await FileSaver.Default.SaveAsync("WorkoutLog.csv", saveStream);
    }
}

public partial class LogFilter : ObservableObject
{
    public event EventHandler? ApplyRequested;

    public LogFilter(RegistrarService registrar)
    {
        WaitAndInit(registrar).SafeFireAndForget(e => e.LogException());
        UserSearch.OnSingleResult += (_, _) => ApplyFilter();
        foreach (var item in Classes)
        {
            item.Selected += (_, _) =>
            {
                if (Classes.All(it => !it.IsSelected))
                    ByClass = false;
                ApplyFilter();
            };
        }
    }

    private async Task WaitAndInit(RegistrarService registrar)
    {
        while (!registrar.UniqueNamesReady)
        {
            await Task.Delay(100);
        }

        UserSearch.SetAvailableUsers(registrar.StudentList);
    }

    [ObservableProperty] bool forCreditOnly;
    [ObservableProperty] bool byClass;
    [ObservableProperty] ObservableCollection<SelectableLabelViewModel> classes = [ "Class V", "Class VI", "Class VII", "Class VIII" ];
    [ObservableProperty] bool byUser;
    [ObservableProperty] UserSearchViewModel userSearch = new();

    internal bool Matches(WorkoutViewModel workout)
    {
        return (!ForCreditOnly || workout.ForCredit)
            && (!ByClass || Classes.Any(cn => cn.IsSelected && workout.Student.DisplayName.Contains($"[{cn.Label}]")))
            && (!ByUser || (UserSearch.IsSelected && UserSearch.Selected.Id == workout.Student.Id));
    }

    [RelayCommand]
    public void ToggleByUser()
    {
        ByUser = !ByUser;
        if (!ByUser)
        {
            UserSearch.ClearSelection();
            ApplyFilter();
        }
    }

    [RelayCommand]
    public void ToggleByClass()
    {
        ByClass = !ByClass;
        if (!ByClass)
        {
            foreach (var cn in Classes)
                cn.IsSelected = false;
            ApplyFilter();
        }
    }

    [RelayCommand]
    public void ToggleForCreditOnly()
    {
        ForCreditOnly = !ForCreditOnly;

        ApplyFilter();
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
        ApplyFilter();
    }

    [RelayCommand]
    public void ApplyFilter() => 
        ApplyRequested?.Invoke(this, EventArgs.Empty);
}
