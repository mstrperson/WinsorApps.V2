using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text.Json;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using ProtoSection = WinsorApps.Services.Bookstore.Models.ProtoSection;

namespace WinsorApps.Services.Bookstore.Services;
public partial class TeacherBookstoreService :
    IAsyncInitService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    private readonly RegistrarService _registrar;

    public TeacherBookstoreService(LocalLoggingService logging, ApiService api, RegistrarService registrar)
    {
        _logging = logging;
        _api = api;
        _registrar = registrar;
    }

    private readonly record struct CacheSchema(
        ImmutableArray<OrderStatus> orderStatusOptions,
        ImmutableArray<OrderOption> orderOptions,
        ImmutableArray<CourseRecord> courses,
        ImmutableArray<ProtoSection> sections,
        ImmutableArray<TeacherBookOrder> myOrders,
        ImmutableArray<SummerSection> summerSections);

    public SchoolYear CurrentBookOrderYear => DateTime.Today.Month >= 3 ?
        _registrar.SchoolYears.First(sy => sy.startDate.Year == DateTime.Today.Year) :
        _registrar.SchoolYears.First(sy => sy.endDate.Year == DateTime.Today.Year);

    public ImmutableArray<OrderStatus> OrderStatusOptions { get; private set; } = [];

    public async Task Initialize(ErrorAction onError)
    {
        await _api.WaitForInit(onError);
        await _registrar.WaitForInit(onError);

        if (Ready)
            return;

        Started = true;
        if (!LoadCache() || (_myProtoSections?.Any(sec => sec.schoolYearId != CurrentBookOrderYear.id) ?? false))
            await Refresh(onError);
        Progress = 1;
        Ready = true;
    }

    public bool Ready { get; protected set; }

    private ImmutableArray<OrderOption>? _orderOptions;
    public ImmutableArray<OrderOption> OrderOptions
    {
        get
        {
            if (!_orderOptions.HasValue || !Ready)
                throw new ServiceNotReadyException(_logging, "Cannot retreive Order Options yet, wait for service to initialize.");

            return _orderOptions.Value;
        }
    }

    private Dictionary<string, List<CourseRecord>>? _coursesByDept;
    public Dictionary<string, List<CourseRecord>> CoursesByDepartment
    {
        get
        {
            if (_coursesByDept is null || !Ready)
                throw new ServiceNotReadyException(_logging, "Cannot retreive Courses yet, wait for service to initialize.");

            return _coursesByDept!;
        }
    }

    private List<ProtoSection>? _myProtoSections;
    public ImmutableArray<ProtoSection> MySections
    {
        get
        {
            if (!Ready) return Enumerable.Empty<ProtoSection>().ToImmutableArray();

            return _myProtoSections?.ToImmutableArray() ?? Enumerable.Empty<ProtoSection>().ToImmutableArray();
        }
    }

    private List<TeacherBookOrder>? _myOrders;
    public ImmutableArray<TeacherBookOrder> MyOrders
    {
        get
        {
            if (!Ready)
                throw new ServiceNotReadyException(_logging, "Cannot retrieve My Orders because the service is not ready yet.");

            return [.. _myOrders!];
        }
    }

    public ImmutableArray<TeacherBookOrder> CurrentYearOrders => MyOrders.Where(ord => ord.schoolYearId == CurrentBookOrderYear.id).ToImmutableArray();

    public string CacheFileName => "teacher-bookstore.cache";

    public bool Started { get; private set; }

    public double Progress { get; private set; }
    public async Task<ProtoSection?> CreateNewSection(string courseId, bool fall, ErrorAction onError)
    {
        var result = await _api.SendAsync<ProtoSection?>(HttpMethod.Post,
            $"api/book-orders/teachers?courseId={courseId}&fall={fall}",
            onError: onError);

        if (result.HasValue)
        {
            if (MySections.All(sec => sec.id != result.Value.id))
            {
                _myProtoSections!.Add(result.Value);
                _myOrders!.Add(new(result.Value.id, result.Value.schoolYearId, result.Value.createdTimeStamp, []));
            }

            SaveCache();
        }

        return result;
    }

    public async Task<ImmutableArray<TeacherBookOrderGroup>> GetGroupedOrders(string sectionId, ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<TeacherBookOrderGroup>>(HttpMethod.Get, $"api/book-orders/teachers/{sectionId}/groups", onError: onError);
        return result;
    }

    public async Task<bool> DeleteSection(string sectionId, ErrorAction onError)
    {
        var success = true;

        await _api.SendAsync(HttpMethod.Delete, $"api/book-orders/teachers/{sectionId}",
            onError: onError);

        var existing = MySections.FirstOrDefault(sec => sec.id == sectionId);
        if (existing != default)
            _myProtoSections!.Remove(existing);

        var order = MyOrders.FirstOrDefault(ord => ord.protoSectionId == sectionId);
        if (order != default)
            _myOrders!.Remove(order);

        SaveCache();
        return success;
    }

    public async Task<TeacherBookOrder?> CreateNewOrder(string sectionId, CreateTeacherBookOrderGroup group, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrderGroup, TeacherBookOrder?>(
            HttpMethod.Post, $"api/book-orders/teachers/{sectionId}/group", group, onError: onError);
        return HandleBookOrderResult(sectionId, result);
    }

    public async Task<TeacherBookOrder?> CreateNewOrder(string sectionId, CreateTeacherBookOrder order, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrder, TeacherBookOrder?>(
            HttpMethod.Post, $"api/book-orders/teachers/{sectionId}", order, onError: onError);
        return HandleBookOrderResult(sectionId, result);
    }

    public async Task<TeacherBookOrder?> UpdateOrder(string sectionId, CreateTeacherBookOrder order, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrder, TeacherBookOrder?>(
            HttpMethod.Put, $"api/book-orders/teachers/{sectionId}", order, onError: onError);
        return HandleBookOrderResult(sectionId, result);
    }
    public async Task<TeacherBookOrder?> UpdateOrder(string sectionId, CreateTeacherBookOrderGroup group, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrderGroup, TeacherBookOrder?>(
            HttpMethod.Put, $"api/book-orders/teachers/{sectionId}/group", group, onError: onError);
        return HandleBookOrderResult(sectionId, result);
    }

    private TeacherBookOrder? HandleBookOrderResult(string sectionId, TeacherBookOrder? result)
    {
        if (result.HasValue)
        {
            if (_myOrders is null)
                _myOrders = [];

            if (_myOrders.Any(ord => ord.protoSectionId == sectionId))
            {
                _myOrders.Replace(_myOrders.First(ord => ord.protoSectionId == sectionId), result.Value);
            }
            else
            {
                _myOrders.Add(result.Value);
            }

            SaveCache();
        }


        return result;
    }

    public async Task<TeacherBookOrder?> DeleteBookOrder(string sectionId, IEnumerable<string> isbns, ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<string>, TeacherBookOrder?>(HttpMethod.Delete,
            $"api/book-orders/teachers/{sectionId}/orders", isbns.ToImmutableArray(), onError: onError);

        return HandleBookOrderResult(sectionId, result);
    }

    public async Task<ImmutableArray<TeacherBookOrderGroup>> GetOrderGroups(string sectionId, ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<TeacherBookOrderGroup>>(HttpMethod.Get,
            $"api/book-orders/teachers/{sectionId}/groups", onError: onError);

        return result;
    }

    public async Task<TeacherBookOrder?> GroupBookOrders(string sectionId, CreateOptionGroup group, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateOptionGroup, TeacherBookOrder?>(HttpMethod.Post,
            $"api/book-orders/teachers/{sectionId}/groups", group, onError: onError);

        return HandleBookOrderResult(sectionId, result);
    }

    public async Task<TeacherBookRequestGroup?> GetGroupDetails(string groupId, ErrorAction onError)
    {
        var result = await _api.SendAsync<TeacherBookRequestGroup?>(HttpMethod.Get,
            $"api/book-orders/teachers/groups/{groupId}", onError: onError);

        return result;
    }

    public void SaveCache()
    {
        CacheSchema cache = new(
            [.. OrderStatusOptions],
            [.. _orderOptions ?? []],
            [.. CoursesByDepartment
                .SelectMany(kvp => kvp.Value)
                .DistinctBy(c => c.courseId)],
            [.. MySections],
            [.. MyOrders],
            [.. SummerSections
                .SelectMany(kvp => kvp.Value)
                .DistinctBy(sec => sec.id)]);

        var json = JsonSerializer.Serialize(cache);
        File.WriteAllText($"{_logging.AppStoragePath}{Path.DirectorySeparatorChar}{CacheFileName}", json);
    }

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

        var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}").Trim();
        try
        {
            var cache = JsonSerializer.Deserialize<CacheSchema>(json);
            OrderStatusOptions = [.. cache.orderStatusOptions];
            _orderOptions = [.. cache.orderOptions];
            _coursesByDept = cache.courses.SeparateByKeys(course => course.department);
            _myProtoSections = [.. cache.sections];
            _myOrders = [.. cache.myOrders];
            SummerSections = cache.summerSections.SeparateByKeys(sec => sec.schoolYear);
            return true;
        }
        catch(Exception e) 
        {
            e.LogException(_logging);
            return false;
        }
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while(!Ready)
        {
            await Task.Delay(100);
        }
    }

    public async Task Refresh(ErrorAction onError)
    {
        var orderOptionTask = _api.SendAsync<ImmutableArray<OrderOption>>(HttpMethod.Get,
            "api/book-orders/order-options", onError: onError);
        orderOptionTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            if (orderOptionTask.IsCompletedSuccessfully)
            {
                _orderOptions = orderOptionTask.Result;
            }
        });

        var statusTask = _api.SendAsync<ImmutableArray<OrderStatus>>(HttpMethod.Get,
            "api/book-orders/teachers/status-list", onError: onError);
        statusTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            OrderStatusOptions = statusTask.Result;
        });

        var courseTask = _api.SendAsync<Dictionary<string, List<CourseRecord>>>(
            HttpMethod.Get, "api/registrar/course/by-department?getsBooks=true", onError: onError);

        courseTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            if (courseTask.IsCompletedSuccessfully)
                _coursesByDept = courseTask.Result!;
        });

        var sectionTask = _api.SendAsync<List<ProtoSection>>(HttpMethod.Get,
            "api/book-orders/teachers/sections", onError: onError);
        sectionTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            if (sectionTask.IsCompletedSuccessfully)
                _myProtoSections = sectionTask.Result;
        });

        var myOrdersTask = _api.SendAsync<List<TeacherBookOrder>>(HttpMethod.Get,
            "api/book-orders/teachers", onError: onError);
        myOrdersTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            _myOrders = myOrdersTask.Result;
        });

        var summerTask = GetSummerSections(onError);

        List<Task> taskList = [myOrdersTask, sectionTask, courseTask, orderOptionTask, statusTask, summerTask];

        while (taskList.Any(t => !t.IsCompleted))
        {
            await Task.Delay(250);
        }
        Ready = true;

        SaveCache();
    }
}