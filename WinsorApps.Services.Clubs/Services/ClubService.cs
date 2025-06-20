using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WinsorApps.Services.Clubs.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Clubs.Services;

public class ClubService(ApiService apiService, LocalLoggingService logging) : IAsyncInitService
{
    private readonly ApiService _apiService = apiService;
    private readonly LocalLoggingService _logging = logging;

    public string ClubId { get; set; } = "";

    public string CacheFileName => "clubs.cache";

    public bool Started { get; private set; }

    public bool Ready { get; private set; }

    public double Progress { get; private set; }

    public List<Club> AllClubs { get; private set; } = [];

    public ClubDetails? SelectedClub { get; private set; } = null;

    public List<ClubAttendanceRecord> OpenAttendances { get; private set; } = [];

    public bool ClubIsSelected => SelectedClub is not null;

    public void ClearCache() 
    { 
        if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) 
            File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); 
    }

    private record CacheStructure(
        string clubId,
        List<Club> allClubs,
        ClubDetails? selectedClub);

    public async Task Initialize(ErrorAction onError)
    {
        Started = true;

        if(LoadCache() && !string.IsNullOrEmpty(ClubId))
        {
            Progress = 1;
            Ready = true;
            return;
        }

        await GetClubs(onError);
        Progress = 0.3;

        while (string.IsNullOrEmpty(ClubId))
            await Task.Delay(250);
        SelectedClub = await GetClubDetails(ClubId, onError);
        Progress = 0.6;
        if (SelectedClub is not null)
        {
            OpenAttendances = (await GetClubAttendance(new DateRange(DateTime.Today.AddDays(-7), DateTime.Today), onError))?.attendances
                .Where(a => a.isOpen).ToList() ?? [];
        }

        await SaveCache();
        Progress = 1;
        Ready = true;
    }

    public async Task<ClubDetails?> GetClubDetails(string clubId, ErrorAction onError) =>
        await _apiService.SendAsync<ClubDetails>(HttpMethod.Get, $"api/clubs/{clubId}/details", onError: onError);

    public async Task<ClubAttendanceCollection?> GetClubAttendance(DateRange range, ErrorAction onError) =>
        !ClubIsSelected
        ? null
        : await _apiService.SendAsync<ClubAttendanceCollection>(
            HttpMethod.Get,
            $"api/clubs/{ClubId}/attendance?start={range.start:yyyy-MM-dd}&end={range.end:yyyy-MM-dd}",
            onError: onError);

    public async Task<Club?> CreateClub(CreateClubRequest request, ErrorAction onError)
    {
        var result = await _apiService.SendAsync<CreateClubRequest, Club>(
            HttpMethod.Post,
            "api/clubs",
            request,
            onError: onError);

        if(result is not null)
        {
            AllClubs.Add(result);
            ClubId = result.id;
            SelectedClub = await GetClubDetails(result.id, onError);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Created Club: {result.name} ({result.id})");
        }
        return result;
    }

    public async Task<ClubAttendanceRecord?> SwipeIn(string cardData, ErrorAction onError, string note = "")
    {
        var result = await _apiService.SendAsync<ClubAttendanceRecord>(HttpMethod.Post,
            $"api/clubs/{ClubId}/attendance?cardData={Uri.EscapeDataString(cardData)}&note={Uri.EscapeDataString($"[Swipe Card] {note}")}",
            onError: onError);

        if(result is not null)
        {
            OpenAttendances.Add(result);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Swiped In: {result.user} ({result.user.blackbaudId}) at {result.timeIn:yyyy-MM-dd HH:mm:ss}");
        }

        return result;
    }
    public async Task<ClubAttendanceRecord?> PunchIn(int blackbaudId, ErrorAction onError, string note = "")
    {
        var result = await _apiService.SendAsync<ClubAttendanceRecord>(HttpMethod.Post,
            $"api/clubs/{ClubId}/attendance/by-id?blackbaudId={blackbaudId}&note={Uri.EscapeDataString($"[Typed ID] {note}")}",
            onError: onError);

        if (result is not null)
        {
            OpenAttendances.Add(result);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Punched In: {result.user} ({result.user.blackbaudId}) at {result.timeIn:yyyy-MM-dd HH:mm:ss}");
        }

        return result;
    }
    public async Task<ClubAttendanceRecord?> SwipeOut(string cardData, ErrorAction onError, string note = "")
    {
        var result = await _apiService.SendAsync<ClubAttendanceRecord>(HttpMethod.Put,
            $"api/clubs/{ClubId}/attendance?cardData={Uri.EscapeDataString(cardData)}&note={Uri.EscapeDataString($"[Swipe Card] {note}")}",
            onError: onError);

        if (result is not null)
        {
            var existing = OpenAttendances.FirstOrDefault(a => a.id == result.id);
            if (existing is not null)
            {
                OpenAttendances.Remove(existing);
            }
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Swiped Out: {result.user} ({result.user.blackbaudId}) at {result.timeOut:yyyy-MM-dd HH:mm:ss}");
        }

        return result;
    }
    public async Task<ClubAttendanceRecord?> PunchOut(int blackbaudId, ErrorAction onError, string note = "")
    {
        var result = await _apiService.SendAsync<ClubAttendanceRecord>(HttpMethod.Put,
            $"api/clubs/{ClubId}/attendance/by-id?blackbaudId={blackbaudId}&note={Uri.EscapeDataString($"[Typed ID] {note}")}",
            onError: onError);

        if (result is not null)
        {
            var existing = OpenAttendances.FirstOrDefault(a => a.id == result.id);
            if (existing is not null)
            {
                OpenAttendances.Remove(existing);
            }
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Punched Out: {result.user} ({result.user.blackbaudId}) at {result.timeOut:yyyy-MM-dd HH:mm:ss}");
        }

        return result;
    }

    public async Task<ClubAttendanceRecord?> CloseAttendance(string attendanceId, ErrorAction onError, string note = "")
    {
        var result = await _apiService.SendAsync<ClubAttendanceRecord>(HttpMethod.Put,
            $"api/clubs/attendance/{attendanceId}?note={Uri.EscapeDataString(note)}",
            onError: onError);
        if (result is not null)
        {
            var existing = OpenAttendances.FirstOrDefault(a => a.id == result.id);
            if (existing is not null)
            {
                OpenAttendances.Remove(existing);
            }
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Closed Attendance: {result.user} ({result.user.blackbaudId}) at {result.timeOut:yyyy-MM-dd HH:mm:ss}");
        }
        return result;
    }

    public async Task GetClubs(ErrorAction onError) => AllClubs = (await _apiService.SendAsync<List<Club>>(HttpMethod.Get, "api/clubs", onError: onError)) ?? [];

    public bool LoadCache()
    {
        if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
            return false;

        var cacheAge = DateTime.Now - File.GetCreationTime($"{_logging.AppStoragePath}{CacheFileName}");

        _logging.LogMessage(LocalLoggingService.LogLevel.Information,
            $"{CacheFileName} is {cacheAge.TotalDays:0.0} days old.");

        if (cacheAge.TotalDays > 14)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Deleting Aged Cache File.");
            File.Delete($"{_logging.AppStoragePath}{CacheFileName}");
            return false;
        }

        var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}"); try
        {
            var cache = JsonSerializer.Deserialize<CacheStructure>(json);
            if (cache is null)
            {
                return false;
            }

            ClubId = cache.clubId;
            AllClubs = cache.allClubs;
            SelectedClub = cache.selectedClub;
        }
        catch
        {
            return false;
        }

        return true;
    }

    public async Task Refresh(ErrorAction onError) => await Initialize(onError);

    public async Task SaveCache()
    {
        int tryCount = 0;
        TryAgain:
        try
        {
            CacheStructure cache = new(ClubId, AllClubs, SelectedClub);

            File.WriteAllText($"{_logging.AppStoragePath}{CacheFileName}", JsonSerializer.Serialize(cache));
        }
        catch (InvalidOperationException ex)
        {
            _logging.LogError(new("Failed to Save Cache", ex.Message));
            if (tryCount < 5)
            {
                tryCount++;
                await Task.Delay(TimeSpan.FromSeconds(15));
                goto TryAgain;
            }
        }
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }
}
