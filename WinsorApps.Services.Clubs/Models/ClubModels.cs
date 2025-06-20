using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Clubs.Models;
public record CreateClubRequest(
    string name,
    string adultId);

public record CreateSwipeCard(
    string b64data,
    bool clearPrevious = true);

public record Club(
    string id,
    string name,
    string schoolYear);
public record ClubDetails(
    string id,
    string name,
    string schoolYear,
    AdvisorRecord adult,
    List<StudentRecordShort> studentLeaders);

public record ClubAttendanceRecord(
    string id,
    UserRecord user,
    DateTime timeIn,
    DateTime? timeOut,
    bool isOpen,
    bool isLeader,
    string note);

public record ClubAttendanceCollection(
    Club club,
    DateRange dates,
    List<ClubAttendanceRecord> attendances);

public record UserClubAttendanceCollection(
    UserRecord user,
    DateRange dates,
    List<ClubAttendanceCollection> attendances);