using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Global.Services
{
    public class RegistrarService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public UserRecord Me => _api.UserInfo!.Value;

        private Dictionary<string, ImmutableArray<SectionDetailRecord>> _sectionsByCourse;

        private async Task DownloadSectionsByCourse(ErrorAction onError)
        {
            _sectionsByCourse = await 
                _api.SendAsync<Dictionary<string, ImmutableArray<SectionDetailRecord>>>(
                    HttpMethod.Get, "api/registrar/course/sections-by-course", onError: onError) ?? [];
        }

        public ImmutableArray<SchoolYear> SchoolYears { get; private set; } = [];

        public RegistrarService(ApiService api, LocalLoggingService logging)
        {
            _api = api;
            _logging = logging;
            _sectionsByCourse = [];
        }


        private ImmutableArray<ScheduleEntry>? _mySchedule { get; set; }

        public async Task<ImmutableArray<ScheduleEntry>> MySchedule()
        {
            try
            {
                if (!_mySchedule.HasValue)
                {
                    _mySchedule = await _api.SendAsync<ImmutableArray<ScheduleEntry>>(HttpMethod.Get, "api/schedule");
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get My Schedule", ex.Message, ex.StackTrace);
                _mySchedule = Array.Empty<ScheduleEntry>().ToImmutableArray();
            }
            return _mySchedule!.Value;
        }

        private ImmutableArray<SectionRecord>? _myAcademicSchedule;
        public async Task<ImmutableArray<SectionRecord>> MyAcademicSchedule()
        {
            try
            {
                if (!_myAcademicSchedule.HasValue)
                {
                    _myAcademicSchedule = await _api.SendAsync<ImmutableArray<SectionRecord>>(HttpMethod.Get, "api/schedule/academics?detailed=true");
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get My Schedule", ex.Message, ex.StackTrace);
                _myAcademicSchedule = Array.Empty<SectionRecord>().ToImmutableArray();
            }
            return _myAcademicSchedule!.Value;
        }

        private ImmutableArray<CourseRecord>? _courses;

        public ImmutableArray<CourseRecord> CourseList
        {
            get
            {
                if (!Ready || !_courses.HasValue)
                    throw new ServiceNotReadyException(_logging, "Course list is not ready.");

                return _courses!.Value;
            }
        }

        public async Task<ImmutableArray<SectionDetailRecord>> GetSectionsOf(CourseRecord course, ErrorAction onError, bool noCache = false)
        {
            if (!noCache && _sectionsByCourse.ContainsKey(course.courseId))
                return _sectionsByCourse[course.courseId];

            var result = await GetSectionsOfFromAPI(course.courseId, onError);
            _sectionsByCourse[course.courseId] = result;
            return result;
        }

        public async Task<ImmutableArray<SectionDetailRecord>> GetSectionsOfFromAPI(string courseId, ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<SectionDetailRecord>>(
                HttpMethod.Get, $"api/registrar/course/{courseId}/sections", 
                onError: onError);

        public async Task<ImmutableArray<CourseRecord>> Courses()
        {
            try
            {
                if (_courses is null)
                {
                    _courses = await _api.SendAsync<ImmutableArray<CourseRecord>>(HttpMethod.Get, "api/registrar/course?getsBooks=true");
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get course list", ex.Message, ex.StackTrace ?? "");
                _courses = Array.Empty<CourseRecord>().ToImmutableArray();
            }

            return _courses!.Value;

        }

        private Dictionary<UserRecord, string> _uniqueNameCache = [];

        private Dictionary<string, SectionDetailRecord?> _sectionDetailCache = new();

        public async Task<SectionDetailRecord?> GetSectionDetails(string sectionId, ErrorAction onError)
        {
            if(_sectionDetailCache.ContainsKey(sectionId))
                return _sectionDetailCache[sectionId];

            var result = await _api.SendAsync<SectionDetailRecord>(HttpMethod.Get, $"api/schedule/academics/{sectionId}", onError: onError);

            _sectionDetailCache[sectionId] = result;
            return result;
        }

        private ImmutableArray<UserRecord>? _teachers;

        public ImmutableArray<UserRecord> TeacherList
        {
            get
            {
                if (!Ready || !_teachers.HasValue)
                    throw new ServiceNotReadyException(_logging, "Teachers list is not yet ready.");

                return _teachers.Value.ToImmutableArray();
            }
        }

        public async Task<ImmutableArray<UserRecord>> Teachers()
        {
            try
            {
                if (_teachers is null || !_teachers.Value.Any())
                {
                    _teachers = await _api.SendAsync<ImmutableArray<UserRecord>>(HttpMethod.Get, "api/users/teachers");
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get teacher list", ex.Message, ex.StackTrace);
                _teachers = Enumerable.Empty<UserRecord>().ToImmutableArray();
            }

            return _teachers.Value;

        }

        public string GetUniqueDisplayNameFor(UserRecord user)
        {
            if(_uniqueNameCache.ContainsKey(user)) 
                return _uniqueNameCache[user];

            var name = AllUsers.GetUniqueNameWithin(user);
            _uniqueNameCache[user] = name;
            return name;
        }

        private IEnumerable<UserRecord>? _employees;
        public ImmutableArray<UserRecord> EmployeeList
        {
            get
            {
                if (!Ready || _employees is null)
                    throw new ServiceNotReadyException(_logging, "Employee list is not yet ready.");

                return _employees.ToImmutableArray();
            }
        }
        public async Task<IEnumerable<UserRecord>> Employees()
        {
            try
            {
                if (_employees is null || !_employees.Any())
                {
                    _employees = await _api.SendAsync<ImmutableArray<UserRecord>>(HttpMethod.Get, "api/users/employees");
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get employee list", ex.Message, ex.StackTrace);
                _employees = Enumerable.Empty<UserRecord>();
            }

            return _employees!;
        }

        private IEnumerable<UserRecord>? _students;
        public ImmutableArray<UserRecord> StudentList
        {
            get
            {
                if (!Ready || _students is null)
                    throw new ServiceNotReadyException(_logging, "Students list is not yet ready.");

                return _students.ToImmutableArray();
            }
        }
        public async Task<IEnumerable<UserRecord>> Students()
        {
            try
            {
                if (_students is null || !_students.Any())
                {
                    _students = await _api.SendAsync<ImmutableArray<UserRecord>>(HttpMethod.Get, "api/users/students");
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get student list", ex.Message, ex.StackTrace);
                _students = Enumerable.Empty<UserRecord>();
            }

            return _students!;

        }

        public ImmutableArray<string> DepartmentList => CourseList.Select(c => c.department).Distinct().ToImmutableArray();

        public async Task<ImmutableArray<string>> Departments() => (await Courses()).Select(c => c.department).Distinct().ToImmutableArray();

        public async Task<ImmutableArray<CourseRecord>> GetDeptCourses(string department) =>
            (await Courses()).Where(c => c.department == department).ToImmutableArray();

        public ImmutableArray<UserRecord> AllUsers =>
            StudentList
            .Union(TeacherList)
            .Union(EmployeeList)
            .Distinct()
            .OrderBy(u => u.lastName)
            .ThenBy(u => u.firstName)
            .ToImmutableArray();

        public bool Ready { get; private set; } = false;

        public SchoolYear GetSchoolYear(string id) => SchoolYears.FirstOrDefault(sy => sy.id == id);

        public async Task Initialize(ErrorAction onError, bool forceReInit = false)
        {
            if (Ready && !forceReInit)
                return; // this has already been initalized...

            while (string.IsNullOrEmpty(_api.AuthUserId))
            {
                Thread.Sleep(500);
            }

            var getSchoolYearTask = _api.SendAsync<ImmutableArray<SchoolYear>>(HttpMethod.Get, "api/schedule/school-years", onError: onError);

            getSchoolYearTask.WhenCompleted(() =>
            {
                SchoolYears = getSchoolYearTask.Result;
            });

            await Task.WhenAll(
                getSchoolYearTask,
                DownloadSectionsByCourse(onError),
                MySchedule(),
                Courses(),
                Teachers(),
                Students(),
                Employees(),
                MyAcademicSchedule());
            
            Ready = true;

            foreach (var user in AllUsers)
                GetUniqueDisplayNameFor(user);
        }

        private ImmutableArray<UserRecord>? _myAdvisees;

        public async Task<ImmutableArray<StudentRecordShort>> GetMyAdvisees(ErrorAction onError)
        {
            if(!_myAdvisees.HasValue)
            {
                _myAdvisees = await _api.SendAsync<ImmutableArray<UserRecord>>(HttpMethod.Get, "api/users/self/advisees", onError: onError);
            }

            return [.._myAdvisees.Value.Select(u => (StudentRecordShort)u)];
        }

        public async Task<Stream> GetUserPhoto(string userId) =>
            await _api.DownloadStream($"api/users/{userId}/photo");

    }

}
