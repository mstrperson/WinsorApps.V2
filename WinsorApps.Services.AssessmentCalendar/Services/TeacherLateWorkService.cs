using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.AssessmentCalendar.Services;
public partial class TeacherAssessmentService
{
    private List<StudentLateWorkCollection> _adviseeLateWork = [];

    public Dictionary<StudentRecordShort, StudentLateWorkCollection> AdviseeLateWork => _adviseeLateWork.ToDictionary(collection => collection.student);

    public async Task<List<StudentLateWorkCollection>> GetAdviseesLateWork(ErrorAction onError, bool includeResolved = false) => 
        await _api.SendAsync<List<StudentLateWorkCollection>>(HttpMethod.Get, $"api/assessment-calendar/late-work/advisees?includeResolved={includeResolved}",
            onError: onError) ?? [];

    public async Task<List<LateWorkDetails>?> GetStudentLateWork(ErrorAction onError, string studentId, bool includeResolved = false) =>
        await _api.SendAsync<List<LateWorkDetails>?>(HttpMethod.Get, $"api/assessment-calendar/late-work/student/{studentId}?includeResolved={includeResolved}",
            onError: onError);

    public async Task<LateWorkByStudentCollection?> PostNewLateAssessment(ErrorAction onError, string assessmentId, NewLateWork lateWork) =>
        await _api.SendAsync<NewLateWork,LateWorkByStudentCollection?>(HttpMethod.Post, $"api/assessment-calendar/late-work/assessment/{assessmentId}", lateWork,
            onError: onError);

    public async Task<LateWorkByStudentCollection?> PostLateWorkPatternFor(ErrorAction onError, string sectionId, NewLateWork lateWork) =>
        await _api.SendAsync<NewLateWork, LateWorkByStudentCollection?>(HttpMethod.Post, $"api/assessment-calendar/late-work/section/{sectionId}", lateWork,
            onError: onError);


    public async Task<List<StudentLateWorkCollection>> GetLateWorkBySection(ErrorAction onError, string sectionId, bool includeResolved = false) =>
        await _api.SendAsync<List<StudentLateWorkCollection>>(HttpMethod.Get, $"api/assessment-calendar/late-work/section/{sectionId}?includeResolved={includeResolved}",
            onError: onError) ?? [];


    public async Task ResolveLateWorkPattern(string lateWorkId, ErrorAction onError) =>
        await _api.SendAsync(HttpMethod.Get, $"api/assessment-calendar/late-work/pattern/{lateWorkId}/resolve",
            onError: onError);

    public async Task ResolveLateAssessment(string lateWorkId, ErrorAction onError) =>
        await _api.SendAsync(HttpMethod.Get, $"api/assessment-calendar/late-work/assessment/{lateWorkId}/resolve",
            onError: onError);

    public Dictionary<(bool, string), LateWorkDetails> LateWorkDetailCache { get; } = [];

    public async Task<LateWorkDetails?> GetLateWorkPattern(ErrorAction onError, string lateWorkId, bool updateCache = false)
    {
        if (LateWorkDetailCache.ContainsKey((false, lateWorkId)) && !updateCache)
            return LateWorkDetailCache[(false, lateWorkId)];
        
        var result = await _api.SendAsync<LateWorkDetails?>(HttpMethod.Get, $"api/assessment-calendar/late-work/pattern/{lateWorkId}",
            onError: onError);
        if (result is null)
            return null;
        LateWorkDetailCache[(false, lateWorkId)] = result;

        return LateWorkDetailCache[(false, lateWorkId)];
    }


    public async Task<LateWorkDetails?> GetLateAssessmentDetails(ErrorAction onError, string lateWorkId, bool updateCache = false)
    {
        if (!LateWorkDetailCache.ContainsKey((true, lateWorkId)) || updateCache)
        {
            var result = await _api.SendAsync<LateWorkDetails?>(HttpMethod.Get, $"api/assessment-calendar/late-work/assessment/{lateWorkId}",
                onError: onError);
            if (result is null)
                return null;
            LateWorkDetailCache[(true, lateWorkId)] = result;

        }

        return LateWorkDetailCache[(true, lateWorkId)];
    }
}

public static partial class Extensions
{
    public static async Task<LateWorkDetails> GetDetails(this LateWorkRecord lw, TeacherAssessmentService service, ErrorAction onError) =>
        lw.isAssessment ? 
            (await service.GetLateAssessmentDetails(onError, lw.id))! :
            (await service.GetLateWorkPattern(onError, lw.id))!;
}