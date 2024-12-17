using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Athletics.Models;
public readonly record struct Workout(string id, UserRecord user, DateTime timeIn, DateTime? timeOut, ImmutableArray<string> workoutDetails);

public readonly record struct WorkoutListItem(string id, DateTime timeIn, DateTime timeOut, bool signedOut)
{
    public static implicit operator WorkoutListItem(Workout signin) => new(signin.id, signin.timeIn, signin.timeOut ?? signin.timeIn, signin.timeOut.HasValue);
}

public readonly record struct StudentWorkoutCollection(StudentRecordShort student, ImmutableArray<WorkoutListItem> workouts, DateRangeWrapper dateRange);

public readonly record struct WorkoutLog(ImmutableArray<Workout> workouts, DateRangeWrapper dateRange);
