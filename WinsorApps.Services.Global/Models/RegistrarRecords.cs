﻿using System.Collections.Immutable;
using System.Diagnostics;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Global.Models;

public record BlockMeetingTime(BlockRecord block, DateTime start, DateTime end);
public record FreeBlockCollection(List<BlockMeetingTime> freeBlocks, DateRange inRange);

/// <summary>
/// An entry in your schedule of any kind.
/// </summary>
/// <param name="id">Id of this entity</param>
/// <param name="name">display name</param>
/// <param name="type">what kind of thing is this? [academic, advisory, activity]</param>
/// <param name="block">what block does this meet in (if applicable)</param>
/// <param name="room">what room does it meet in</param>
public record ScheduleEntry(string id, string name, string type, string block, string room);

/// <summary>
/// details only relevant to students.
/// </summary>
/// <param name="gradYear">What year do they graduate</param>
/// <param name="className">What is their current class</param>
/// <param name="advisor">Who is their advisor (full contact details)</param>
public record StudentInfoRecord(int gradYear, string className, AdvisorRecord? advisor = null);

public record AdvisorRecord(string id, int blackbaudId, string firstName, string nickname, string lastName, string email)
{
    public override string ToString() => string.IsNullOrWhiteSpace(nickname) || nickname == firstName ?
            $"{firstName} {lastName}" : $"{nickname} {lastName}";

    public static implicit operator AdvisorRecord(UserRecord user) =>
        new(user.id, user.blackbaudId, user.firstName, user.nickname, user.lastName, user.email);

    public static implicit operator UserRecord(AdvisorRecord user) =>
        new(user.id, user.blackbaudId, user.firstName, user.nickname, user.lastName, user.email);
}

/// <summary>
/// User Details useful to your app
/// </summary>
/// <param name="blackbaudId">this person's Blackbaud UserId</param>
/// <param name="firstName">Given Name</param>
/// <param name="nickname">Chosen Prefered Name (from WILD)</param>
/// <param name="lastName">Family Name</param>
/// <param name="email">Email address used for logging in and communication.</param>
/// <param name="studentInfo"></param>
public record UserRecord(string id = "", int blackbaudId = -1, string firstName = "", 
    string nickname = "", string lastName = "", string email = "",
    StudentInfoRecord? studentInfo = null, bool hasLogin = false, DocumentHeader? photo = null)
{
    public static readonly UserRecord Empty = new();
    internal bool requiresYearDistinction { get; } = false;
    internal bool requiresNameDistinction { get; } = false;
    public override string ToString()
    {
        var name = string.IsNullOrWhiteSpace(nickname) || nickname == firstName ?
            $"{firstName} {lastName}" : $"{nickname} {lastName}";

        if (studentInfo is not null)
        {
            name += $" [{studentInfo.className}]";
        }
        return name;
    }

    public async Task<List<string>> GetRoles(ApiService api) => await api.SendAsync<List<string>>(HttpMethod.Get, "api/users/self/roles") ?? [];
}
public record StudentRecordShort(
    string id, 
    string displayName, 
    string email, 
    int gradYear, 
    string className, 
    string advisorName)
{
    public static implicit operator StudentRecordShort(UserRecord student) =>
        student.studentInfo is null ? 
        new StudentRecordShort(student.id, $"{student.firstName} {student.lastName}", student.email, 0, "N/A", "N/A"):
        new StudentRecordShort(student.id,
            student.requiresNameDistinction ?
                student.firstName == student.nickname ?
                    $"{student.firstName} {student.lastName}" :
                    $"{student.nickname} ({student.firstName}) {student.lastName}" :
            student.requiresYearDistinction ?
                $"{student.nickname} {student.lastName} [{student.studentInfo?.className}]" :
                $"{student.nickname} {student.lastName}",
            student.email,
            student.studentInfo?.gradYear ?? 0, student.studentInfo?.className ?? "", student.studentInfo?.advisor is null ? "" :
            $"{student.studentInfo?.advisor?.firstName} {student.studentInfo?.advisor?.lastName}");

    public override string ToString() => $"{displayName} [{className}]";
}

/// <summary>
/// A school term
/// </summary>
/// <param name="termId">Term Id used for scheduling</param>
/// <param name="schoolYear">School Year of the term</param>
/// <param name="name">Display name (First Semester, Second Semester, etc.)</param>
/// <param name="start">term begin date</param>
/// <param name="end">term end date</param>
public record TermRecord(string termId, string schoolYear, string name, DateTime start, DateTime end)
{
    public bool IsCurrent() => start <= DateTime.Today && end >= DateTime.Today;
}

/// <summary>
/// Course details used for scheduling
/// </summary>
/// <param name="courseId">Course Id (used for creating new assessments)</param>
/// <param name="courseCode">Course Code from WILD</param>
/// <param name="displayName">Course Name</param>
/// <param name="lengthInTerms">1 semester, or full year</param>
/// <param name="department">What department is this course listed in.</param>
public record CourseRecord(string courseId, string courseCode, string displayName, int lengthInTerms, string department)
{
    public static readonly CourseRecord Empty = new("", "", "", 0, "");
    public override string ToString() => this.displayName;
}

/// <summary>
/// Detailed Record of a section with expanded results
/// </summary>
/// <param name="sectionId">Section Id used when scheduling an assessment.</param>
/// <param name="course">Course Details <see cref="CourseRecord"/></param>
/// <param name="primaryTeacher">Who is listed as the primary teacher? <see cref="UserRecord"/></param>
/// <param name="teachers">List of ther teachers.</param>
/// <param name="students">List of students on the roster with full details including who is their advisor.</param>
/// <param name="term">What term does this section belong to. <see cref="TermRecord"/></param>
/// <param name="room">Where does it meet. <see cref="RoomRecord"/></param>
/// <param name="block">What block does it meet in (if it is scheduled)<see cref="BlockRecord"/></param>
public record SectionDetailRecord(string sectionId, CourseRecord course, UserRecord primaryTeacher,
    List<AdvisorRecord> teachers, List<StudentRecordShort> students,
    TermRecord term, RoomRecord? room, BlockRecord? block, bool isCurrent)
{
    public string displayName
    {
        get
        {
            var dn = course.displayName;
            if (this.block is not null)
                dn += $" [{block?.name}]";

            return dn;
        }
    }

    public static implicit operator SectionRecord(SectionDetailRecord sec) =>
        new(sec.sectionId, sec.course.courseId, sec.primaryTeacher.id,
            [.. sec.teachers],
            [.. sec.students],
            sec.term.termId, 
            sec.room?.name ?? "", 
            sec.block?.name ?? "", 
            sec.block?.blockId ?? "",
            sec.displayName, 
            "",
            sec.isCurrent);
}

/// <summary>
/// Condensed Section Record with only Ids listed instead of full details.
/// </summary>
/// <param name="sectionId"></param>
/// <param name="courseId"></param>
/// <param name="primaryTeacherId"></param>
/// <param name="teachers">list of teacher Ids</param>
/// <param name="students">list of student Ids</param>
/// <param name="termId"></param>
/// <param name="room">room name</param>
/// <param name="block">block name</param>
/// <param name="displayName">Section Display (as seen on your Google Calendar)</param>
public record SectionRecord(string sectionId, string courseId, string? primaryTeacherId,
    List<AdvisorRecord> teachers, List<StudentRecordShort> students, string termId,
    string room, string block, string blockId, string displayName, string schoolLevel, bool isCurrent)
{
    public static readonly SectionRecord Empty = new("", "", "", [], [], "", "", "", "", "", "", false);
}

public record SectionMinimalRecord(string sectionId, string courseId, string? primaryTeacherId,
    List<string> teachers, List<string> students, string termId,
    string room, string block, string blockId, string displayName, string schoolLevel, bool isCurrent);

/// <summary>
/// Information about a room
/// </summary>
/// <param name="roomId">Id used in scheduling</param>
/// <param name="name">Room name</param>
/// <param name="googleCalendarId">Google Resource Calendar Id (incase you want to look at that directly.)</param>
public record RoomRecord(string roomId, string name, string googleCalendarId);

/// <summary>
/// Block information
/// </summary>
/// <param name="blockId">Id used in scheduling</param>
/// <param name="name">Block name</param>
/// <param name="schoolLevel">Upper School blocks are different from Lower School blocks.</param>
public record BlockRecord(string blockId, string name, string schoolLevel);


public record DocumentHeader(string id, string fileName, string mimeType, string location);

public static partial class Extensions
{
    public static string GetUniqueNameWithin(this List<string> names, UserRecord user)
    {
        var name = $"{user}";
        if (!names.Contains(name))
        {
            return name;
        }

        if (names.Distinct().Count(u => name == $"{u}") > 1)
        {
            name = $"{user.firstName} {name}";
            names.Add(name);
        }

        if (names.Distinct().Count(u => name == $"{u}") > 1)
        {
            name = $"{user.nickname} \"{user.firstName}\" {name}";
        }

        //Debug.WriteLine($"Found unique name {name}");
        return name;
    }

    public static string GetUniqueNameWithin(this IEnumerable<UserRecord> users, UserRecord user)
    {
        Debug.WriteLine($"Looking up unique name for {user}");
        var names = users.Select(u => $"{u}").ToList();
        return names.GetUniqueNameWithin(user);

    }
}

