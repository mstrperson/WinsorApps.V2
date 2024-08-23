using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using AsyncAwaitBestPractices;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Global.Services
{
    public class RegistrarService : IAsyncInitService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        /// <summary>
        /// The Currently Logged-in User.
        /// </summary>
        public UserRecord Me => _api.UserInfo!.Value;
        public ImmutableArray<string> MyRoles { get; private set; } = [];

        private Dictionary<string, ConcurrentBag<SectionDetailRecord>> _sectionsByCourse;

        /// <summary>
        /// School Year data cache
        /// </summary>
        public ConcurrentBag<SchoolYear> SchoolYears { get; private set; } = [];

        public RegistrarService(ApiService api, LocalLoggingService logging)
        {
            _api = api;
            _logging = logging;
            _sectionsByCourse = [];
        }
        
        public async Task Refresh(ErrorAction onError)
        {
            Started = false;
            Ready = false;
            await Initialize(onError);
        }

        /// <summary>
        /// Sections by Course Cache download.
        /// </summary>
        /// <param name="onError"></param>
        private async Task DownloadSectionsByCourseAsync(ErrorAction onError)
        {
            var result = await 
                _api.SendAsync<Dictionary<string, ImmutableArray<SectionDetailRecord>>>(
                    HttpMethod.Get, "api/registrar/course/sections-by-course", onError: onError) ?? [];

            _sectionsByCourse = new();
            foreach (var key in result.Keys)
            {
                _sectionsByCourse.Add(key, [.. result[key]]);
            }
        }



        /// <summary>
        /// My Schedule Cache.
        /// </summary>
        private ConcurrentBag<ScheduleEntry> _mySchedule { get; set; } = [];

        /// <summary>
        /// Access the MySchedule cache.
        /// </summary>
        public ConcurrentBag<ScheduleEntry> MySchedule => _mySchedule ?? [];
        /// <summary>
        /// Get My Schedule from the API.  Stores it in Cache.
        /// </summary>
        /// <returns></returns>
        public async Task<ConcurrentBag<ScheduleEntry>> GetMyScheduleAsync()
        {
            try
            {
                if (_mySchedule.IsEmpty)
                {
                    _mySchedule = [..await _api.SendAsync<ImmutableArray<ScheduleEntry>>(HttpMethod.Get, "api/schedule")];
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get My Schedule", ex.Message, ex.StackTrace);
                _mySchedule = [];
            }
            return _mySchedule;
        }

        /// <summary>
        /// My Academic Schedule Cache.
        /// </summary>
        private ConcurrentBag<SectionRecord> _myAcademicSchedule = [];

        /// <summary>
        /// Access My Academic Schedule cache.
        /// </summary>
        public ConcurrentBag<SectionRecord> MyAcademicSchedule => _myAcademicSchedule ?? [];
        
        public async Task<ConcurrentBag<SectionRecord>> GetMyAcademicScheduleAsync()
        {
            try
            {
                if (_myAcademicSchedule.IsEmpty)
                {
                    _myAcademicSchedule = [..await _api.SendAsync<ImmutableArray<SectionRecord>>(HttpMethod.Get, "api/schedule/academics?detailed=true")];
                    foreach (var section in _myAcademicSchedule)
                        _ = await GetSectionDetailsAsync(section.sectionId, _logging.LogError);
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get My Schedule", ex.Message, ex.StackTrace);
                _myAcademicSchedule = [];
            }
            return _myAcademicSchedule;
        }

        /// <summary>
        /// Course Data Cache.
        /// </summary>
        private ConcurrentBag<CourseRecord> _courses = [];

        /// <summary>
        /// Access the Course Data Cache.  Throws an exception if the service is not yet initalized.
        /// </summary>
        /// <exception cref="ServiceNotReadyException"></exception>
        public ConcurrentBag<CourseRecord> CourseList
        {
            get
            {
                if (!Ready)
                    throw new ServiceNotReadyException(_logging, "Course list is not ready.");

                return _courses;
            }
        }

        /// <summary>
        /// Get Sections of a given Course uses Cache if available, otherwise, asks the API to fill in the data.
        /// </summary>
        /// <param name="course"></param>
        /// <param name="onError"></param>
        /// <param name="noCache"></param>
        /// <returns></returns>
        public async Task<ConcurrentBag<SectionDetailRecord>> GetSectionsOfAsync(CourseRecord course, ErrorAction onError, bool noCache = false)
        {
            if (!noCache && _sectionsByCourse.TryGetValue(course.courseId, out var async))
                return async;

            var result = await GetSectionsOfFromAPIAsync(course.courseId, onError);
            _sectionsByCourse[course.courseId] = [..result];
            return [..result];
        }

        /// <summary>
        /// Get Sections of a given Course (from the current Academic Year) from the API.
        /// only used if updating Cache is required.
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        private async Task<ConcurrentBag<SectionDetailRecord>> GetSectionsOfFromAPIAsync(string courseId, ErrorAction onError) =>
            [..await _api.SendAsync<ImmutableArray<SectionDetailRecord>>(
                HttpMethod.Get, $"api/registrar/course/{courseId}/sections", 
                onError: onError)];

        /// <summary>
        /// Get Course Data from the API.  Only used during init.
        /// </summary>
        /// <returns></returns>
        private async Task<ConcurrentBag<CourseRecord>> GetCoursesAsync()
        {
            try
            {
                if (_courses.IsEmpty)
                {
                    _courses = [..await _api.SendAsync<ImmutableArray<CourseRecord>>(HttpMethod.Get, "api/registrar/course?getsBooks=true")];
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get course list", ex.Message, ex.StackTrace ?? "");
                _courses = [];
            }

            return _courses;

        }


        /// <summary>
        /// Section Detail Cache.  Saves overhead on API Calls
        /// </summary>
        public Dictionary<string, SectionDetailRecord> SectionDetailCache { get; private set; } = [];

        /// <summary>
        /// Get Details for a section from the API.  First checks cached data, and calls API, if this data has not
        /// been previously requested.
        /// </summary>
        /// <param name="sectionId"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<SectionDetailRecord?> GetSectionDetailsAsync(string sectionId, ErrorAction onError)
        {
            if(SectionDetailCache.ContainsKey(sectionId))
                return SectionDetailCache[sectionId];

            var result = await _api.SendAsync<SectionDetailRecord?>(HttpMethod.Get, $"api/schedule/academics/{sectionId}", onError: onError);

            if(result.HasValue)
                SectionDetailCache[sectionId] = result.Value;
            return result;
        }

        /// <summary>
        /// Teacher Data Cache
        /// </summary>
        private ConcurrentBag<UserRecord> _teachers = [];

        /// <summary>
        /// get Teacher user data Cache.  Throws an exception if accessed before init has completed.
        /// </summary>
        /// <exception cref="ServiceNotReadyException"></exception>
        public ConcurrentBag<UserRecord> TeacherList
        {
            get
            {
                if (!Ready)
                    throw new ServiceNotReadyException(_logging, "GetTeachersAsync list is not yet ready.");

                return _teachers;
            }
        }

        /// <summary>
        /// Get all Teacher data from API.  Only used during Init.
        /// </summary>
        /// <returns></returns>
        private async Task<ConcurrentBag<UserRecord>> GetTeachersAsync()
        {
            try
            {
                if (_teachers.IsEmpty)
                {
                    _teachers = [..await _api.SendAsync<ImmutableArray<UserRecord>>(HttpMethod.Get, "api/users/teachers")];
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get teacher list", ex.Message, ex.StackTrace);
                _teachers = [];
            }

            return _teachers;

        }
        
        
        /// <summary>
        /// User Unique Name Cache, maps a user record to thier Unique display name.
        /// saves complex Calculations when displaying a list of users.
        /// </summary>
        private Dictionary<UserRecord, string> _uniqueNameCache = [];

        /// <summary>
        /// Unique Display Name for a given user within the AllUsers cache.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public string GetUniqueDisplayNameFor(UserRecord user)
        {
            if(_uniqueNameCache.ContainsKey(user)) 
                return _uniqueNameCache[user];

            var name = AllUsers.GetUniqueNameWithin(user);
            _uniqueNameCache[user] = name;
            return name;
        }

        /// <summary>
        /// Employee Data Cache.
        /// </summary>
        private ConcurrentBag<UserRecord> _employees = [];
        
        /// <summary>
        /// Access the Employee Data cache.  throws an exception if initialization has not completed.
        /// </summary>
        /// <exception cref="ServiceNotReadyException"></exception>
        public ConcurrentBag<UserRecord> EmployeeList
        {
            get
            {
                if (!Ready)
                    throw new ServiceNotReadyException(_logging, "Employee list is not yet ready.");

                return _employees;
            }
        }
        
        /// <summary>
        /// Get all Employee data from the API.  Only used during Init.
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<UserRecord>> GetEmployeesAsync()
        {
            try
            {
                if (_employees.IsEmpty)
                {
                    _employees = [..await _api.SendAsync<ImmutableArray<UserRecord>>(HttpMethod.Get, "api/users/employees")];
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get employee list", ex.Message, ex.StackTrace);
                _employees = [];
            }

            return _employees;
        }

        /// <summary>
        /// Student data cache.
        /// </summary>
        private ConcurrentBag<UserRecord> _students = [];
        
        /// <summary>
        /// Get the Student list Cache, throws an exception if the service is not ready yet.
        /// </summary>
        /// <exception cref="ServiceNotReadyException"></exception>
        public ConcurrentBag<UserRecord> StudentList
        {
            get
            {
                if (!Ready)
                    throw new ServiceNotReadyException(_logging, "GetStudentsAsync list is not yet ready.");

                return _students;
            }
        }
        
        /// <summary>
        /// Get All Student Users from the API.  Only used during Init.
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<UserRecord>> GetStudentsAsync()
        {
            try
            {
                if (_students.IsEmpty)
                {
                    _students = [..await _api.SendAsync<ImmutableArray<UserRecord>>(HttpMethod.Get, "api/users/students")];
                }
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to get student list", ex.Message, ex.StackTrace);
                _students = [];
            }

            return _students;

        }

        /// <summary>
        /// Get Department List synchronously from the CourseList Cache
        /// </summary>
        public ImmutableArray<string> DepartmentList => [..CourseList.Select(c => c.department).Distinct()];

        /// <summary>
        /// Get the list of all Courses in a given department.
        /// </summary>
        /// <param name="department"></param>
        /// <returns></returns>
        public ImmutableArray<CourseRecord> GetDeptCourses(string department) =>
            [..CourseList.Where(c => c.department == department)];

        /// <summary>
        /// Get All Users from the Application Cache by means of appending all different lists together.
        /// </summary>
        public ConcurrentBag<UserRecord> AllUsers =>
            [..StudentList
            .Union(TeacherList)
            .Union(EmployeeList)
            .Distinct()
            .OrderBy(u => u.lastName)
            .ThenBy(u => u.firstName)];

        
        /// <summary>
        /// Get School Year info from a given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SchoolYear GetSchoolYear(string id) => SchoolYears.FirstOrDefault(sy => sy.id == id);
        
        /// <summary>
        /// Flag that is set once the Initialize method has run to completion successfully.
        /// </summary>
        public bool Ready { get; private set; } = false;

        public double Progress { get; private set; } = 0;
        
        public bool Started { get; private set; }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!this.Ready)
            {
                await Task.Delay(250);
            }
        }

        /// <summary>
        /// Initialize the Registrar Service.
        /// This should be called after a user has logged into the application.
        /// </summary>
        /// <param name="onError"></param>
        public async Task Initialize(ErrorAction onError)
        {
            if (Ready)
                return; // this has already been initalized...

            if (Started)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, "Api Initialization called multiple times...");
                while (!Ready)
                {
                    await Task.Delay(250);
                }
                return;
            }
            
            Started = true;
            while (string.IsNullOrEmpty(_api.AuthUserId))
            {
                Thread.Sleep(500);
            }

            var getSchoolYearTask = _api.SendAsync<ImmutableArray<SchoolYear>>(HttpMethod.Get, "api/schedule/school-years", onError: onError);

            getSchoolYearTask.WhenCompleted(() =>
            {
                SchoolYears = [..getSchoolYearTask.Result];
            });
            var sbc = DownloadSectionsByCourseAsync(onError);
            var mySched = GetMyScheduleAsync();
            var getCourses = GetCoursesAsync();
            var getTeachers = GetTeachersAsync();
            var getStudents = GetStudentsAsync();
            var getEmployees = GetEmployeesAsync();
            var acad = GetMyAcademicScheduleAsync();

            ImmutableArray<Task> tasks =
                [getSchoolYearTask, sbc, mySched, getCourses, getTeachers, getStudents, getEmployees, acad];
            
            foreach(var task in tasks)
                task.WhenCompleted(() =>
                {
                    Progress += 1.0 / tasks.Length;
                });
            
            await Task.WhenAll(
                getSchoolYearTask,
                sbc,
                mySched,
                getCourses,
                getTeachers,
                getStudents,
                getEmployees,
                acad);

            MyRoles = await Me.GetRoles(_api);

            Progress = 1;
            Ready = true;

            GetUniqueNames().SafeFireAndForget(e => e.LogException(_logging));
        }

        public bool UniqueNamesReady { get; private set; } = false;

        public async Task WaitForUniqueNames()
        {
            while (!UniqueNamesReady)
                await Task.Delay(100);
        }
        private async Task GetUniqueNames() => await Task.Run(() =>
        {
            foreach (var user in AllUsers)
                GetUniqueDisplayNameFor(user);
            var check = _uniqueNameCache.Values.Count == _uniqueNameCache.Values.Distinct().Count();
            if (!check)
            {
                var missed = _uniqueNameCache
                    .Where(kvp => _uniqueNameCache.Values.ToImmutableArray().Count(name => kvp.Value == name) > 1)
                    .SeparateByKeys(kvp => kvp.Value);

                foreach (var name in missed.Keys)
                {
                    Debug.WriteLine($"The Name {name} still appears {missed[name].Count} times...");
                    foreach (var user in missed[name].Select(kvp => kvp.Key))
                    {
                        if (!string.IsNullOrEmpty(user.nickname) && user.nickname != user.firstName)
                        {
                            var fixedName = $"{user.nickname} \"{user.firstName}\" {user.lastName}";
                            if (user.studentInfo.HasValue)
                                fixedName += $" [{user.studentInfo.Value.className}]";
                            _uniqueNameCache[user] = fixedName;

                            Debug.WriteLine($"Got {fixedName} corrected!");
                        }
                    }
                }
            }
            UniqueNamesReady = true;
        });
        
        

        /// <summary>
        /// Cache for MyAdvisees
        /// </summary>
        private ImmutableArray<UserRecord>? _myAdvisees;
        
        /// <summary>
        /// Access the saved cache directly.  Returns empty list if cache has not been populated.
        /// </summary>
        public ImmutableArray<UserRecord> MyAdvisees => _myAdvisees ?? [];

        /// <summary>
        /// Teachers only, Get Your List of Advisees
        /// Students will get NoContent
        /// </summary>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<ImmutableArray<StudentRecordShort>> GetMyAdviseesAsync(ErrorAction onError)
        {
            if(!_myAdvisees.HasValue)
            {
                _myAdvisees = await _api.SendAsync<ImmutableArray<UserRecord>>(HttpMethod.Get, "api/users/self/advisees", onError: onError);
            }

            return [.._myAdvisees.Value.Select(u => (StudentRecordShort)u)];
        }

        /// <summary>
        /// Get a user photo from WILD
        /// </summary>
        /// <param name="userId">ID of the User you're looking for.</param>
        /// <returns>Stream containing the data for a file to render or save locally</returns>
        public async Task<FileContentResult?> GetUserPhotoAsync(string userId, ErrorAction onError) =>
            await _api.SendAsync<FileContentResult?>(HttpMethod.Get, $"api/users/{userId}/photo", onError: onError);

        public async Task<ImmutableArray<SectionRecord>> GetAcademicScheduleFor(string userId, ErrorAction onError)
        {
            var result = await _api.SendAsync<ImmutableArray<SectionRecord>?>(HttpMethod.Get,
                $"api/schedule/academics/for/{userId}?detailed=true", onError: onError);

            return result ?? [];
        }
    }


}
