using System.Collections.Immutable;
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

        private Dictionary<string, ImmutableArray<SectionDetailRecord>> _sectionsByCourse;

        /// <summary>
        /// School Year data cache
        /// </summary>
        public ImmutableArray<SchoolYear> SchoolYears { get; private set; } = [];

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
            _sectionsByCourse = await 
                _api.SendAsync<Dictionary<string, ImmutableArray<SectionDetailRecord>>>(
                    HttpMethod.Get, "api/registrar/course/sections-by-course", onError: onError) ?? [];
        }



        /// <summary>
        /// My Schedule Cache.
        /// </summary>
        private ImmutableArray<ScheduleEntry>? _mySchedule { get; set; }

        /// <summary>
        /// Access the MySchedule cache.
        /// </summary>
        public ImmutableArray<ScheduleEntry> MySchedule => _mySchedule ?? [];
        /// <summary>
        /// Get My Schedule from the API.  Stores it in Cache.
        /// </summary>
        /// <returns></returns>
        public async Task<ImmutableArray<ScheduleEntry>> GetMyScheduleAsync()
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

        /// <summary>
        /// My Academic Schedule Cache.
        /// </summary>
        private ImmutableArray<SectionRecord>? _myAcademicSchedule;

        /// <summary>
        /// Access My Academic Schedule cache.
        /// </summary>
        public ImmutableArray<SectionRecord> MyAcademicSchedule => _myAcademicSchedule ?? [];
        
        public async Task<ImmutableArray<SectionRecord>> GetMyAcademicScheduleAsync()
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

        /// <summary>
        /// Course Data Cache.
        /// </summary>
        private ImmutableArray<CourseRecord>? _courses;

        /// <summary>
        /// Access the Course Data Cache.  Throws an exception if the service is not yet initalized.
        /// </summary>
        /// <exception cref="ServiceNotReadyException"></exception>
        public ImmutableArray<CourseRecord> CourseList
        {
            get
            {
                if (!Ready || !_courses.HasValue)
                    throw new ServiceNotReadyException(_logging, "Course list is not ready.");

                return _courses!.Value;
            }
        }

        /// <summary>
        /// Get Sections of a given Course uses Cache if available, otherwise, asks the API to fill in the data.
        /// </summary>
        /// <param name="course"></param>
        /// <param name="onError"></param>
        /// <param name="noCache"></param>
        /// <returns></returns>
        public async Task<ImmutableArray<SectionDetailRecord>> GetSectionsOfAsync(CourseRecord course, ErrorAction onError, bool noCache = false)
        {
            if (!noCache && _sectionsByCourse.ContainsKey(course.courseId))
                return _sectionsByCourse[course.courseId];

            var result = await GetSectionsOfFromAPIAsync(course.courseId, onError);
            _sectionsByCourse[course.courseId] = result;
            return result;
        }

        /// <summary>
        /// Get Sections of a given Course (from the current Academic Year) from the API.
        /// only used if updating Cache is required.
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        private async Task<ImmutableArray<SectionDetailRecord>> GetSectionsOfFromAPIAsync(string courseId, ErrorAction onError) =>
            await _api.SendAsync<ImmutableArray<SectionDetailRecord>>(
                HttpMethod.Get, $"api/registrar/course/{courseId}/sections", 
                onError: onError);

        /// <summary>
        /// Get Course Data from the API.  Only used during init.
        /// </summary>
        /// <returns></returns>
        private async Task<ImmutableArray<CourseRecord>> GetCoursesAsync()
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

        
        /// <summary>
        /// Section Detail Cache.  Saves overhead on API Calls
        /// </summary>
        private Dictionary<string, SectionDetailRecord?> _sectionDetailCache = new();

        /// <summary>
        /// Get Details for a section from the API.  First checks cached data, and calls API, if this data has not
        /// been previously requested.
        /// </summary>
        /// <param name="sectionId"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<SectionDetailRecord?> GetSectionDetailsAsync(string sectionId, ErrorAction onError)
        {
            if(_sectionDetailCache.ContainsKey(sectionId))
                return _sectionDetailCache[sectionId];

            var result = await _api.SendAsync<SectionDetailRecord>(HttpMethod.Get, $"api/schedule/academics/{sectionId}", onError: onError);

            _sectionDetailCache[sectionId] = result;
            return result;
        }

        /// <summary>
        /// Teacher Data Cache
        /// </summary>
        private ImmutableArray<UserRecord>? _teachers;

        /// <summary>
        /// get Teacher user data Cache.  Throws an exception if accessed before init has completed.
        /// </summary>
        /// <exception cref="ServiceNotReadyException"></exception>
        public ImmutableArray<UserRecord> TeacherList
        {
            get
            {
                if (!Ready || !_teachers.HasValue)
                    throw new ServiceNotReadyException(_logging, "GetTeachersAsync list is not yet ready.");

                return _teachers.Value.ToImmutableArray();
            }
        }

        /// <summary>
        /// Get all Teacher data from API.  Only used during Init.
        /// </summary>
        /// <returns></returns>
        private async Task<ImmutableArray<UserRecord>> GetTeachersAsync()
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
        private IEnumerable<UserRecord>? _employees;
        
        /// <summary>
        /// Access the Employee Data cache.  throws an exception if initialization has not completed.
        /// </summary>
        /// <exception cref="ServiceNotReadyException"></exception>
        public ImmutableArray<UserRecord> EmployeeList
        {
            get
            {
                if (!Ready || _employees is null)
                    throw new ServiceNotReadyException(_logging, "Employee list is not yet ready.");

                return _employees.ToImmutableArray();
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

        /// <summary>
        /// Student data cache.
        /// </summary>
        private IEnumerable<UserRecord>? _students;
        
        /// <summary>
        /// Get the Student list Cache, throws an exception if the service is not ready yet.
        /// </summary>
        /// <exception cref="ServiceNotReadyException"></exception>
        public ImmutableArray<UserRecord> StudentList
        {
            get
            {
                if (!Ready || _students is null)
                    throw new ServiceNotReadyException(_logging, "GetStudentsAsync list is not yet ready.");

                return _students.ToImmutableArray();
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

        /// <summary>
        /// Get Department List synchronously from the CourseList Cache
        /// </summary>
        public ImmutableArray<string> DepartmentList => CourseList.Select(c => c.department).Distinct().ToImmutableArray();

        /// <summary>
        /// Get Departments directly from API (requires an API Call)
        /// </summary>
        /// <returns></returns>
        public async Task<ImmutableArray<string>> GetDepartmentsAsync() => (await GetCoursesAsync()).Select(c => c.department).Distinct().ToImmutableArray();

        /// <summary>
        /// Get the list of all Courses in a given department.
        /// </summary>
        /// <param name="department"></param>
        /// <returns></returns>
        public ImmutableArray<CourseRecord> GetDeptCourses(string department) =>
            CourseList.Where(c => c.department == department).ToImmutableArray();

        /// <summary>
        /// Get All Users from the Application Cache by means of appending all different lists together.
        /// </summary>
        public ImmutableArray<UserRecord> AllUsers =>
            StudentList
            .Union(TeacherList)
            .Union(EmployeeList)
            .Distinct()
            .OrderBy(u => u.lastName)
            .ThenBy(u => u.firstName)
            .ToImmutableArray();

        
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
            if (Ready) return;

            if (!this.Started)
                await this.Initialize(onError);

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
                SchoolYears = getSchoolYearTask.Result;
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
            
            Ready = true;

            foreach (var user in AllUsers)
                GetUniqueDisplayNameFor(user);
        }

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

    }

}
