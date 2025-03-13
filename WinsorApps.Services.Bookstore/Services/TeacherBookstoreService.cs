using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text.Json;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using ProtoSection = WinsorApps.Services.Bookstore.Models.ProtoSection;

namespace WinsorApps.Services.Bookstore.Services;
public partial class TeacherBookstoreService(LocalLoggingService logging, ApiService api, RegistrarService registrar) :
    IAsyncInitService
{
    private readonly ApiService _api = api;
    private readonly LocalLoggingService _logging = logging;
    private readonly RegistrarService _registrar = registrar;

    private record CacheSchema(
        List<OrderStatus> orderStatusOptions,
        List<OrderOption> orderOptions,
        List<CourseRecord> courses,
        List<ProtoSection> sections,
        List<TeacherBookOrder> myOrders,
        List<SummerSection> summerSections);

    public SchoolYear CurrentBookOrderYear => DateTime.Today.Month >= 3 ?
        _registrar.SchoolYears.First(sy => sy.startDate.Year == DateTime.Today.Year) :
        _registrar.SchoolYears.First(sy => sy.endDate.Year == DateTime.Today.Year);

    public List<OrderStatus> OrderStatusOptions { get; private set; } = [];

    public async Task Initialize(ErrorAction onError)
    {
        await _api.WaitForInit(onError);
        await _registrar.WaitForInit(onError);

        if (Ready)
            return;

        Started = true;
        if (!LoadCache() || MySections.Any(sec => sec.schoolYearId != CurrentBookOrderYear.id))
            await Refresh(onError);
        Progress = 1;
        Ready = true;
    }

    public bool Ready { get; protected set; }

    public List<OrderOption> OrderOptions { get; private set; } = [];
    
    public Dictionary<string, List<CourseRecord>> CoursesByDepartment { get; private set; } = [];

    public List<ProtoSection> MySections { get; private set; } = [];

    public List<TeacherBookOrder> MyOrders { get; private set; } = [];

    public List<TeacherBookOrder> CurrentYearOrders => MyOrders.Where(ord => ord.schoolYearId == CurrentBookOrderYear.id).ToList();

    public string CacheFileName => "teacher-bookstore.cache";

    public bool Started { get; private set; }

    public double Progress { get; private set; }
    public async Task<ProtoSection?> CreateNewSection(string courseId, bool fall, ErrorAction onError)
    {
        var result = await _api.SendAsync<ProtoSection?>(HttpMethod.Post,
            $"api/book-orders/teachers?courseId={courseId}&fall={fall}",
            onError: onError);

        if (result is not null)
        {
            if (MySections.All(sec => sec.id != result.id))
            {
                MySections.Add(result);
                MyOrders.Add(new(result.id, result.schoolYearId, result.createdTimeStamp, []));
            }

            await SaveCache();
        }

        return result;
    }

    public async Task<List<TeacherBookOrderGroup>> GetGroupedOrders(string sectionId, ErrorAction onError)
    {
        var result = await _api.SendAsync<List<TeacherBookOrderGroup>>(HttpMethod.Get, $"api/book-orders/teachers/{sectionId}/groups", onError: onError);
        return result ?? [];
    }

    public async Task<bool> DeleteSection(string sectionId, ErrorAction onError)
    {
        var success = true;

        await _api.SendAsync(HttpMethod.Delete, $"api/book-orders/teachers/{sectionId}",
            onError: onError);

        var existing = MySections.FirstOrDefault(sec => sec.id == sectionId);
        if (existing is not null)
            MySections.Remove(existing);

        var order = MyOrders.FirstOrDefault(ord => ord.protoSectionId == sectionId);
        if (order is not null)
            MyOrders.Remove(order);

        await SaveCache();
        return success;
    }

    public async Task<TeacherBookOrder?> CreateNewOrder(string sectionId, CreateTeacherBookOrderGroup group, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrderGroup, TeacherBookOrder?>(
            HttpMethod.Post, $"api/book-orders/teachers/{sectionId}/group", group, onError: onError);
        return await HandleBookOrderResult(sectionId, result);
    }

    public async Task<TeacherBookOrder?> CreateNewOrder(string sectionId, CreateTeacherBookOrder order, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrder, TeacherBookOrder?>(
            HttpMethod.Post, $"api/book-orders/teachers/{sectionId}", order, onError: onError);
        return await HandleBookOrderResult(sectionId, result);
    }

    public async Task<TeacherBookOrder?> UpdateOrder(string sectionId, CreateTeacherBookOrder order, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrder, TeacherBookOrder?>(
            HttpMethod.Put, $"api/book-orders/teachers/{sectionId}", order, onError: onError);
        return await HandleBookOrderResult(sectionId, result);
    }
    public async Task<TeacherBookOrder?> UpdateOrder(string sectionId, CreateTeacherBookOrderGroup group, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrderGroup, TeacherBookOrder?>(
            HttpMethod.Put, $"api/book-orders/teachers/{sectionId}/group", group, onError: onError);
        return await HandleBookOrderResult(sectionId, result);
    }

    private async Task<TeacherBookOrder?> HandleBookOrderResult(string sectionId, TeacherBookOrder? result)
    {
        if (result is not null)
        {
            if (MyOrders.Any(ord => ord.protoSectionId == sectionId))
            {
                MyOrders.Replace(MyOrders.First(ord => ord.protoSectionId == sectionId), result);
            }
            else
            {
                MyOrders.Add(result);
            }

            await SaveCache();
        }


        return result;
    }

    public async Task<TeacherBookOrder?> DeleteBookOrder(string sectionId, IEnumerable<string> isbns, ErrorAction onError)
    {
        var result = await _api.SendAsync<List<string>, TeacherBookOrder?>(HttpMethod.Delete,
            $"api/book-orders/teachers/{sectionId}/orders", isbns.ToList(), onError: onError);

        return await HandleBookOrderResult(sectionId, result);
    }

    public async Task<List<TeacherBookOrderGroup>> GetOrderGroups(string sectionId, ErrorAction onError)
    {
        var result = await _api.SendAsync<List<TeacherBookOrderGroup>>(HttpMethod.Get,
            $"api/book-orders/teachers/{sectionId}/groups", onError: onError);

        return result ?? [];
    }

    public async Task<TeacherBookOrder?> GroupBookOrders(string sectionId, CreateOptionGroup group, ErrorAction onError)
    {
        var result = await _api.SendAsync<CreateOptionGroup, TeacherBookOrder?>(HttpMethod.Post,
            $"api/book-orders/teachers/{sectionId}/groups", group, onError: onError);

        return await HandleBookOrderResult(sectionId, result);
    }

    public async Task<TeacherBookRequestGroup?> GetGroupDetails(string groupId, ErrorAction onError)
    {
        var result = await _api.SendAsync<TeacherBookRequestGroup?>(HttpMethod.Get,
            $"api/book-orders/teachers/groups/{groupId}", onError: onError);

        return result;
    }

    public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }
    public async Task SaveCache()
    {
        CacheSchema cache = new(
            [.. OrderStatusOptions],
            [.. OrderOptions ?? []],
            [.. CoursesByDepartment
                .SelectMany(kvp => kvp.Value)
                .DistinctBy(c => c.courseId)],
            [.. MySections],
            [.. MyOrders],
            [.. SummerSections
                .SelectMany(kvp => kvp.Value)
                .DistinctBy(sec => sec.id)]);

        var json = JsonSerializer.Serialize(cache);
        await File.WriteAllTextAsync($"{_logging.AppStoragePath}{Path.DirectorySeparatorChar}{CacheFileName}", json);
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
            if (cache is null) return false;

            OrderStatusOptions = [.. cache.orderStatusOptions];
            OrderOptions = [.. cache.orderOptions];
            CoursesByDepartment = cache.courses.SeparateByKeys(course => course.department);
            MySections = [.. cache.sections];
            MyOrders = [.. cache.myOrders];
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
        var orderOptionTask = _api.SendAsync<List<OrderOption>>(HttpMethod.Get,
            "api/book-orders/order-options", onError: onError);
        orderOptionTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            if (orderOptionTask.IsCompletedSuccessfully)
            {
                OrderOptions = orderOptionTask.Result ?? [];
            }
        });

        var statusTask = _api.SendAsync<List<OrderStatus>>(HttpMethod.Get,
            "api/book-orders/teachers/status-list", onError: onError);
        statusTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            OrderStatusOptions = statusTask.Result ?? [];
        });

        var courseTask = _api.SendAsync<Dictionary<string, List<CourseRecord>>>(
            HttpMethod.Get, "api/registrar/course/by-department?getsBooks=true", onError: onError);

        courseTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            if (courseTask.IsCompletedSuccessfully)
                CoursesByDepartment = courseTask.Result ?? [];
        });

        var sectionTask = _api.SendAsync<List<ProtoSection>>(HttpMethod.Get,
            "api/book-orders/teachers/sections", onError: onError);
        sectionTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            if (sectionTask.IsCompletedSuccessfully)
                MySections = sectionTask.Result ?? [];
        });

        var myOrdersTask = _api.SendAsync<List<TeacherBookOrder>>(HttpMethod.Get,
            "api/book-orders/teachers", onError: onError);
        myOrdersTask.WhenCompleted(() =>
        {
            Progress += 0.2;
            MyOrders = myOrdersTask.Result ?? [];
        });

        var summerTask = GetSummerSections(onError);

        List<Task> taskList = [myOrdersTask, sectionTask, courseTask, orderOptionTask, statusTask, summerTask];

        while (taskList.Any(t => !t.IsCompleted))
        {
            await Task.Delay(250);
        }
        Ready = true;

        await SaveCache();
    }
}