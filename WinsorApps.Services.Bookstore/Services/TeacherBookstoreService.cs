using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using SectionRecord = WinsorApps.Services.Bookstore.Models.SectionRecord;

namespace WinsorApps.Services.Bookstore.Services;
public partial class TeacherBookstoreService
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

    public SchoolYear CurrentBookOrderYear => DateTime.Today.Month >= 4 ?
        _registrar.SchoolYears.First(sy => sy.startDate.Year == DateTime.Today.Year) :
        _registrar.SchoolYears.First(sy => sy.endDate.Year == DateTime.Today.Year);

    public ImmutableArray<OrderStatus> OrderStatusOptions { get; private set; } = [];

    public async Task Initialize(ErrorAction onError, bool rerun = false)
    {
        if (Ready && !rerun)
            return;

        var orderOptionTask = _api.SendAsync<ImmutableArray<OrderOption>>(HttpMethod.Get,
            "api/book-orders/order-options", onError: onError);
        orderOptionTask.WhenCompleted(() =>
        {
            if (orderOptionTask.IsCompletedSuccessfully)
            {
                _orderOptions = orderOptionTask.Result;
            }
        });

        var statusTask = _api.SendAsync<ImmutableArray<OrderStatus>>(HttpMethod.Get,
            "api/book-orders/teachers/status-list", onError: onError);
        statusTask.WhenCompleted(() =>
        {
            OrderStatusOptions = statusTask.Result;
        });

        var courseTask = _api.SendAsync<Dictionary<string, ImmutableArray<CourseRecord>>>(
            HttpMethod.Get, "api/registrar/course/by-department?getsBooks=true", onError: onError);

        courseTask.WhenCompleted(() =>
        {
            if (courseTask.IsCompletedSuccessfully)
                _coursesByDept = new ReadOnlyDictionary<string, ImmutableArray<CourseRecord>>(courseTask.Result!);
        });

        var sectionTask = _api.SendAsync<List<SectionRecord>>(HttpMethod.Get,
            "api/book-orders/teachers/sections", onError: onError);
        sectionTask.WhenCompleted(() =>
        {
            if (sectionTask.IsCompletedSuccessfully)
                _myProtoSections = sectionTask.Result;
        });

        var myOrdersTask = _api.SendAsync<List<TeacherBookOrder>>(HttpMethod.Get,
            "api/book-orders/teachers", onError: onError);
        myOrdersTask.WhenCompleted(() =>
        {
            _myOrders = myOrdersTask.Result;
        });

        List<Task> taskList = [myOrdersTask, sectionTask, courseTask, orderOptionTask, statusTask];

        while (taskList.Any(t => !t.IsCompleted))
        {
            await Task.Delay(250);
        }

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

    private ReadOnlyDictionary<string, ImmutableArray<CourseRecord>>? _coursesByDept;
    public ReadOnlyDictionary<string, ImmutableArray<CourseRecord>> CoursesByDepartment
    {
        get
        {
            if (_coursesByDept is null || !Ready)
                throw new ServiceNotReadyException(_logging, "Cannot retreive Courses yet, wait for service to initialize.");

            return _coursesByDept!;
        }
    }

    private List<SectionRecord>? _myProtoSections;
    public ImmutableArray<SectionRecord> MySections
    {
        get
        {
            if (!Ready) return Enumerable.Empty<SectionRecord>().ToImmutableArray();

            return _myProtoSections?.ToImmutableArray() ?? Enumerable.Empty<SectionRecord>().ToImmutableArray();
        }
    }

    private List<TeacherBookOrder>? _myOrders;
    public ImmutableArray<TeacherBookOrder> MyOrders
    {
        get
        {
            if (!Ready)
                throw new ServiceNotReadyException(_logging, "Cannot retrieve My Orders because the service is not ready yet.");

            return _myOrders!.ToImmutableArray();
        }
    }

    public ImmutableArray<TeacherBookOrder> CurrentYearOrders => MyOrders.Where(ord => ord.schoolYearId == CurrentBookOrderYear.id).ToImmutableArray();

    public async Task<SectionRecord?> CreateNewSection(string courseId, ErrorAction onError)
    {
        var result = await _api.SendAsync<SectionRecord?>(HttpMethod.Post,
            $"api/book-orders/teachers?courseId={courseId}",
            onError: onError);

        if (result.HasValue)
        {
            if (MySections.All(sec => sec.id != result.Value.id))
            {
                _myProtoSections!.Add(result.Value);
                _myOrders!.Add(new(result.Value.id, result.Value.schoolYearId, result.Value.createdTimeStamp, Array.Empty<TeacherBookRequest>().ToImmutableArray()));
            }
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
                _myOrders = new();

            if (_myOrders.Any(ord => ord.protoSectionId == sectionId))
            {
                _myOrders.Replace(_myOrders.First(ord => ord.protoSectionId == sectionId), result.Value);
            }
            else
            {
                _myOrders.Add(result.Value);
            }
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
}