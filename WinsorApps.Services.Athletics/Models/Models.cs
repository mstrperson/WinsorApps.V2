using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Athletics.Models;
public record Workout(string id, UserRecord user, DateTime timeIn, DateTime? timeOut, List<string> workoutDetails, bool invalidated = false)
{
    public static Workout Empty => new("", UserRecord.Empty, default, null, [], false);
}

public record WorkoutListItem(string id, DateTime timeIn, DateTime timeOut, bool signedOut)
{
    public static implicit operator WorkoutListItem(Workout signin) => new(signin.id, signin.timeIn, signin.timeOut ?? signin.timeIn, signin.timeOut is not null);
}

public record StudentWorkoutCollection(StudentRecordShort student, List<WorkoutListItem> workouts, DateRangeWrapper dateRange);

public record WorkoutLog(List<Workout> workouts, DateRangeWrapper dateRange);
