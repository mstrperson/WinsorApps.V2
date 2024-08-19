using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using SectionRecord = WinsorApps.Services.Bookstore.Models.SectionRecord;

namespace WinsorApps.Services.Bookstore.Services;

public partial class BookstoreManagerService :
    IAsyncInitService,
    IAutoRefreshingCacheService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    private readonly RegistrarService _registrar;
    private readonly BookService _bookService;
    public readonly TeacherBookstoreService TeacherBookstoreService;

    public event EventHandler? OnCacheRefreshed;

    public ImmutableArray<SectionRecord> ProtoSections { get; protected set; } = [];

    public ImmutableArray<TeacherBookOrder> OrderCache { get; protected set; } = [];

    public Dictionary<string, List<TeacherBookOrderDetail>> OrdersByTeacher
    {
        get
        {
            var orders = OrderCache.Select(order =>
                new TeacherBookOrderDetail(ProtoSections.First(sec => sec.id == order.protoSectionId), order.books));
            return orders.SeparateByKeys(order => order.section.teacherId);
        }
    }

    public Dictionary<string, List<SectionRecord>> SectionsByTeacher => ProtoSections.SeparateByKeys(sec => sec.teacherId);

    public bool Ready { get; private set; } = false;

    public bool Started { get; protected set; }

    public double Progress { get; protected set; }

    public TimeSpan RefreshInterval => TimeSpan.FromMinutes(5);

    public bool Refreshing { get; private set; }

    public BookstoreManagerService(RegistrarService registrar, LocalLoggingService logging,
            ApiService api, BookService bookService, TeacherBookstoreService teacherBookstoreService)
    {
        _registrar = registrar;
        _logging = logging;
        _api = api;
        _bookService = bookService;

        Initialize(err => { }).SafeFireAndForget(e => e.LogException(_logging));
        TeacherBookstoreService = teacherBookstoreService;
    }

    public async Task Initialize(ErrorAction onError)
    {
        while (!(_registrar.Ready && _bookService.Ready))
            await Task.Delay(500);

        var cacheTask = _api.SendAsync<ImmutableArray<TeacherBookOrder>>(HttpMethod.Get, "api/book-orders/manager/requests", onError: onError);
        cacheTask.WhenCompleted(() =>
        {
            OrderCache = cacheTask.Result;
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Bookstore Manager Task => OrderCache loaded.");
        });

        var sectionTask = _api.SendAsync<ImmutableArray<SectionRecord>>(HttpMethod.Get, "api/book-orders/manager/sections", onError: onError);
        sectionTask.WhenCompleted(() =>
        {
            ProtoSections = sectionTask.Result;
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Bookstore Manager Task => Sections loaded.");
        });

        await Task.WhenAll(cacheTask, sectionTask);

        Ready = true;
    }


    public async Task<TeacherBookOrderCollection> GetTeacherBookOrders(UserRecord teacher, ErrorAction onError)
    {
        var sections = await GetTeacherSections(teacher.id, onError);

        var orders = OrderCache
            .Where(order => sections.Any(sec => sec.id == order.protoSectionId))
            .Select(order => new TeacherBookOrderDetail(
                sections.First(sec => sec.id == order.protoSectionId),
                order.books))
            .ToImmutableArray();


        return new TeacherBookOrderCollection(teacher, orders);
    }

    public async Task<ImmutableArray<SectionRecord>> GetTeacherSections(string teacherId, ErrorAction onError, bool forceUpdate = false)
    {
        if (!forceUpdate && SectionsByTeacher.ContainsKey(teacherId))
            return SectionsByTeacher[teacherId].ToImmutableArray();

        var result = await _api.SendAsync<ImmutableArray<SectionRecord>>(HttpMethod.Get,
            $"api/book-orders/teachers/{teacherId}/sections", onError: onError);

        SectionsByTeacher[teacherId] = result.ToList();
        return result;
    }

    public async Task<ImmutableArray<TeacherBookOrderGroup>> GetGroupedOrders(string sectionId, ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<TeacherBookOrderGroup>>(HttpMethod.Get, $"api/book-orders/teachers/{sectionId}/groups", onError: onError);
        return result;
    }

    public async Task DeleteSection(string sectionId, ErrorAction onError)
    {
        bool error = false;
        await _api.SendAsync(HttpMethod.Delete, $"api/book-orders/sections/{sectionId}", onError: err =>
        {
            error = true;
            onError(err);
        });
        if (error) return;

        var deletedOrders = OrderCache.Where(ord => ord.protoSectionId == sectionId).ToList();
        foreach (var del in deletedOrders)
            OrderCache = OrderCache.Remove(del);

        if (SectionsByTeacher.Any(kvp => kvp.Value.Any(sec => sec.id == sectionId)))
        {
            var pair = SectionsByTeacher.First(kvp => kvp.Value.Any(sec => sec.id == sectionId));
            SectionsByTeacher[pair.Key].Remove(pair.Value.First(sec => sec.id == sectionId));
        }
    }

    public async Task<SectionRecord?> CreateSectionForTeacher(string teacherId, string courseId, ErrorAction onError)
    {
        var newSection = await _api.SendAsync<SectionRecord?>(HttpMethod.Post,
            $"api/book-orders/teachers/{teacherId}/sections?courseId={courseId}",
            onError: onError);
        if (!newSection.HasValue)
            return null;

        if (!SectionsByTeacher.ContainsKey(teacherId))
            SectionsByTeacher[teacherId] = new();
        SectionsByTeacher[teacherId].Add(newSection.Value);
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        return newSection.Value;
    }

    public async Task<TeacherBookOrderDetail?> CreateOrUpdateBookOrder(
        string teacherId, string sectionId, CreateTeacherBookOrder order, ErrorAction onError, bool update = false)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrder, TeacherBookOrder?>(
            update ? HttpMethod.Put : HttpMethod.Post,
            $"api/book-orders/teachers/{sectionId}", order, onError: onError);

        if (!result.HasValue)
            return null;


        if (!SectionsByTeacher.ContainsKey(teacherId) || !SectionsByTeacher[teacherId].Any(sec => sec.id == sectionId))
        {
            onError(new("Unreachable Error", "Something went wrong... you shouldn't be able to see this message...  Please submit your logs on the Help Page."));
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug,
                $"Successfully submitted a new teacher book order, but I couldn't find data coresponding to teacher: {teacherId} and section: {sectionId} in the cache...");
            return null;
        }

        var orderDetail = new TeacherBookOrderDetail(SectionsByTeacher[teacherId].First(sec => sec.id == sectionId), result.Value.books);
        if (!OrdersByTeacher.ContainsKey(teacherId))
        {
            OrdersByTeacher[teacherId] = [orderDetail];
            return orderDetail;
        }

        if (OrdersByTeacher[teacherId].Any(ord => ord.section.id == sectionId))
            OrdersByTeacher[teacherId].Remove(OrdersByTeacher[teacherId].First(ord => ord.section.id == sectionId));

        OrdersByTeacher[teacherId].Add(orderDetail);
        return orderDetail;
    }
    public async Task<TeacherBookOrderDetail?> CreateOrUpdateBookOrder(
        string teacherId, string sectionId, CreateTeacherBookOrderGroup order, ErrorAction onError, bool update = false)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrderGroup, TeacherBookOrder?>(
            update ? HttpMethod.Put : HttpMethod.Post,
            $"api/book-orders/teachers/{sectionId}/group", order, onError: onError);

        if (!result.HasValue)
            return null;


        if (!SectionsByTeacher.ContainsKey(teacherId) || !SectionsByTeacher[teacherId].Any(sec => sec.id == sectionId))
        {
            onError(new("Unreachable Error", "Something went wrong... you shouldn't be able to see this message...  Please submit your logs on the Help Page."));
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug,
                $"Successfully submitted a new teacher book order, but I couldn't find data coresponding to teacher: {teacherId} and section: {sectionId} in the cache...");
            return null;
        }

        var orderDetail = new TeacherBookOrderDetail(SectionsByTeacher[teacherId].First(sec => sec.id == sectionId), result.Value.books);
        if (!OrdersByTeacher.ContainsKey(teacherId))
        {
            OrdersByTeacher[teacherId] = [orderDetail];
            return orderDetail;
        }

        if (OrdersByTeacher[teacherId].Any(ord => ord.section.id == sectionId))
            OrdersByTeacher[teacherId].Remove(OrdersByTeacher[teacherId].First(ord => ord.section.id == sectionId));

        OrdersByTeacher[teacherId].Add(orderDetail);
        return orderDetail;
    }
    public async Task<ImmutableArray<TeacherBookOrderCollection>> GetOrdersByDepartment(string department, ErrorAction onError, bool updateCache = false)
    {
        if (updateCache)
        {
            await Initialize(onError);
        }

        var sections = ProtoSections.Where(sec => sec.course.department == department).ToImmutableArray();
        return GetBookOrderCollectionsByTeacher(sections);
    }

    public async Task<ImmutableArray<TeacherBookOrderCollection>> GetOrdersByCourse(string courseId, ErrorAction onError, bool updateCache = false)
    {
        if (updateCache)
        {
            await Initialize(onError);
        }

        var sections = ProtoSections.Where(sec => sec.course.courseId == courseId);
        return GetBookOrderCollectionsByTeacher(sections);
    }

    private ImmutableArray<TeacherBookOrderCollection> GetBookOrderCollectionsByTeacher(IEnumerable<SectionRecord> sections)
    {
        Dictionary<UserRecord, List<TeacherBookOrderDetail>> dict = new();
        foreach (var section in sections)
        {
            var teacher = _registrar.TeacherList.First(t => t.id == section.teacherId);
            if (!dict.ContainsKey(teacher))
                dict.Add(teacher, new());

            var sectionOrder = OrdersByTeacher[teacher.id].FirstOrDefault(order => order.section.id == section.id);

            dict[teacher].Add(sectionOrder);
        }

        List<TeacherBookOrderCollection> output = new();
        foreach (var teacher in dict.Keys)
            output.Add(new(teacher, dict[teacher].ToImmutableArray()));

        return output.ToImmutableArray();
    }

    public async Task<ImmutableArray<BookOrderReportEntry>> GetBookOrderReport(ErrorAction onError)
        => await _api.SendAsync<ImmutableArray<BookOrderReportEntry>>(HttpMethod.Get,
            "api/book-orders/manager/report", onError: onError);

    public async Task<byte[]> DownloadReportCSV(ErrorAction onError, bool fall = true, bool spring = false, bool byIsbn = false)
         => await _api.DownloadFile($"api/book-orders/manager/requests/csv?fall={fall}&spring={spring}&byIsbn={byIsbn}",
             onError: onError);

    public async Task<byte[]> DownloadOdinDataTemplate(ErrorAction onError) =>
        await _api.DownloadFile("api/books/manager/odin-data-template",
            onError: onError);

    public async Task<byte[]> BulkUploadOdinData(FileStreamWrapper fileStream, ErrorAction onError) =>
        await _api.DownloadFile("api/books/manager/odin-data", inStream: fileStream,
            onError: onError);

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }

    public async Task Refresh(ErrorAction onError)
    {
        if (Refreshing) return;

        Refreshing = true;

        var cacheTask = _api.SendAsync<ImmutableArray<TeacherBookOrder>>(HttpMethod.Get, "api/book-orders/manager/requests", onError: onError);
        cacheTask.WhenCompleted(() =>
        {
            OrderCache = cacheTask.Result;
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Bookstore Manager Task => OrderCache loaded.");
        });

        var sectionTask = _api.SendAsync<ImmutableArray<SectionRecord>>(HttpMethod.Get, "api/book-orders/manager/sections", onError: onError);
        sectionTask.WhenCompleted(() =>
        {
            ProtoSections = sectionTask.Result;
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Bookstore Manager Task => Sections loaded.");
        });

        await Task.WhenAll(cacheTask, sectionTask);

        Refreshing = false;
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RefreshInBackground(CancellationToken token, ErrorAction onError)
    {
        while(!token.IsCancellationRequested)
        {
            await Task.Delay(RefreshInterval);
            await Refresh(onError);
        }
    }
}