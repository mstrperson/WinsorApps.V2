using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using AsyncAwaitBestPractices;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using ProtoSection = WinsorApps.Services.Bookstore.Models.ProtoSection;

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

    public List<ProtoSection> ProtoSections { get; protected set; } = [];

    public List<TeacherBookOrder> OrderCache { get; protected set; } = [];

    public Dictionary<string, List<TeacherBookOrderDetail>> OrdersByTeacher
    {
        get
        {
            var orders = OrderCache.Select(order =>
                new TeacherBookOrderDetail(ProtoSections.First(sec => sec.id == order.protoSectionId), order.books));
            return orders.SeparateByKeys(order => order.section.teacherId);
        }
    }

    public Dictionary<string, List<ProtoSection>> SectionsByTeacher => ProtoSections.SeparateByKeys(sec => sec.teacherId);

    public bool Ready { get; private set; } = false;

    public bool Started { get; protected set; }

    public double Progress { get; protected set; }

    public TimeSpan RefreshInterval => TimeSpan.FromMinutes(5);

    public bool Refreshing { get; private set; }

    public string CacheFileName => throw new NotImplementedException();

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
        while (!(_api.Ready && _registrar.Ready && _bookService.Ready))
            await Task.Delay(500);

        var cacheTask = _api.SendAsync<List<TeacherBookOrder>>(HttpMethod.Get, "api/book-orders/manager/requests", onError: onError);
        cacheTask.WhenCompleted(() =>
        {
            OrderCache = cacheTask.Result ?? [];
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Bookstore Manager Task => OrderCache loaded.");
        });

        var sectionTask = _api.SendAsync<List<ProtoSection>>(HttpMethod.Get, "api/book-orders/manager/sections", onError: onError);
        sectionTask.WhenCompleted(() =>
        {
            ProtoSections = sectionTask.Result ?? [];
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
            .ToList();


        return new TeacherBookOrderCollection(teacher, orders);
    }

    public async Task<List<ProtoSection>> GetTeacherSections(string teacherId, ErrorAction onError, bool forceUpdate = false)
    {
        if (!forceUpdate && SectionsByTeacher.TryGetValue(teacherId, out var cached))
            return [.. cached];

        var result = await _api.SendAsync<List<ProtoSection>>(HttpMethod.Get,
            $"api/book-orders/teachers/{teacherId}/sections", onError: onError) ?? [];

        SectionsByTeacher[teacherId] = [.. result];
        return result;
    }

    public async Task<List<TeacherBookOrderGroup>> GetGroupedOrders(string sectionId, ErrorAction onError)
    {
        var result = await _api.SendAsync<List<TeacherBookOrderGroup>>(HttpMethod.Get, $"api/book-orders/teachers/{sectionId}/groups", onError: onError) ?? [];
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
            OrderCache.Remove(del);

        if (SectionsByTeacher.Any(kvp => kvp.Value.Any(sec => sec.id == sectionId)))
        {
            var pair = SectionsByTeacher.First(kvp => kvp.Value.Any(sec => sec.id == sectionId));
            SectionsByTeacher[pair.Key].Remove(pair.Value.First(sec => sec.id == sectionId));
        }
    }

    public async Task<ProtoSection?> CreateSectionForTeacher(string teacherId, string courseId, ErrorAction onError)
    {
        var newSection = await _api.SendAsync<ProtoSection?>(HttpMethod.Post,
            $"api/book-orders/teachers/{teacherId}/sections?courseId={courseId}",
            onError: onError);
        if (newSection is null)
            return null;

        var val = SectionsByTeacher.GetOrAdd(teacherId, []);
        val.Add(newSection);
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        return newSection;
    }

    public async Task<TeacherBookOrderDetail?> CreateOrUpdateBookOrder(
        string teacherId, string sectionId, CreateTeacherBookOrder order, ErrorAction onError, bool update = false)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrder, TeacherBookOrder?>(
            update ? HttpMethod.Put : HttpMethod.Post,
            $"api/book-orders/teachers/{sectionId}", order, onError: onError);

        if (result is null)
            return null;


        if (!SectionsByTeacher.TryGetValue(teacherId, out var cached) || !cached.Any(sec => sec.id == sectionId))
        {
            onError(new("Unreachable Error", "Something went wrong... you shouldn't be able to see this message...  Please submit your logs on the Help Page."));
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug,
                $"Successfully submitted a new teacher book order, but I couldn't find data coresponding to teacher: {teacherId} and section: {sectionId} in the cache...");
            return null;
        }

        var orderDetail = new TeacherBookOrderDetail(cached.First(sec => sec.id == sectionId), result.books);
        
        var ordersByTeacher = OrdersByTeacher.GetOrAdd(teacherId, [orderDetail]);

        ordersByTeacher.ReplaceBy(orderDetail, ord =>
        {
            if (Unsafe.AreSame(ref orderDetail, ref ord))
                return false;



            return true;
        });

        if (ordersByTeacher.Any(ord => ord.section.id == sectionId))
            ordersByTeacher.Remove(ordersByTeacher.First(ord => ord.section.id == sectionId));
            
        OrdersByTeacher[teacherId].Add(orderDetail);
        return orderDetail;
    }
    public async Task<TeacherBookOrderDetail?> CreateOrUpdateBookOrder(
        string teacherId, string sectionId, CreateTeacherBookOrderGroup order, ErrorAction onError, bool update = false)
    {
        var result = await _api.SendAsync<CreateTeacherBookOrderGroup, TeacherBookOrder?>(
            update ? HttpMethod.Put : HttpMethod.Post,
            $"api/book-orders/teachers/{sectionId}/group", order, onError: onError);

        if (result is null)
            return null;


        if (!SectionsByTeacher.ContainsKey(teacherId) || !SectionsByTeacher[teacherId].Any(sec => sec.id == sectionId))
        {
            onError(new("Unreachable Error", "Something went wrong... you shouldn't be able to see this message...  Please submit your logs on the Help Page."));
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug,
                $"Successfully submitted a new teacher book order, but I couldn't find data coresponding to teacher: {teacherId} and section: {sectionId} in the cache...");
            return null;
        }

        var orderDetail = new TeacherBookOrderDetail(SectionsByTeacher[teacherId].First(sec => sec.id == sectionId), result.books);
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
    public async Task<List<TeacherBookOrderCollection>> GetOrdersByDepartment(string department, ErrorAction onError, bool updateCache = false)
    {
        if (updateCache)
        {
            await Initialize(onError);
        }

        var sections = ProtoSections.Where(sec => sec.course.department == department).ToList();
        return GetBookOrderCollectionsByTeacher(sections);
    }

    public async Task<List<TeacherBookOrderCollection>> GetOrdersByCourse(string courseId, ErrorAction onError, bool updateCache = false)
    {
        if (updateCache)
        {
            await Initialize(onError);
        }

        var sections = ProtoSections.Where(sec => sec.course.courseId == courseId);
        return GetBookOrderCollectionsByTeacher(sections);
    }

    private List<TeacherBookOrderCollection> GetBookOrderCollectionsByTeacher(IEnumerable<ProtoSection> sections)
    {
        Dictionary<UserRecord, List<TeacherBookOrderDetail>> dict = [];
        foreach (var section in sections)
        {
            var teacher = _registrar.TeacherList.First(t => t.id == section.teacherId);
            if (!dict.ContainsKey(teacher))
                dict.Add(teacher, []);

            var sectionOrder = OrdersByTeacher[teacher.id].FirstOrDefault(order => order.section.id == section.id);
            if(sectionOrder is not null)
                dict[teacher].Add(sectionOrder);
        }

        List<TeacherBookOrderCollection> output = [];
        foreach (var teacher in dict.Keys)
            output.Add(new(teacher, [.. dict[teacher]]));

        return [.. output];
    }

    public async Task<List<BookOrderReportEntry>> GetBookOrderReport(ErrorAction onError)
        => await _api.SendAsync<List<BookOrderReportEntry>>(HttpMethod.Get,
            "api/book-orders/manager/report", onError: onError) ?? [];

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

        var cacheTask = _api.SendAsync<List<TeacherBookOrder>>(HttpMethod.Get, "api/book-orders/manager/requests", onError: onError);
        cacheTask.WhenCompleted(() =>
        {
            OrderCache = cacheTask.Result ?? [];
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Bookstore Manager Task => OrderCache loaded.");
        });

        var sectionTask = _api.SendAsync<List<ProtoSection>>(HttpMethod.Get, "api/book-orders/manager/sections", onError: onError);
        sectionTask.WhenCompleted(() =>
        {
            ProtoSections = sectionTask.Result ?? [];
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

    public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }
    public async Task SaveCache()
    {
        throw new NotImplementedException();
    }

    public bool LoadCache()
    {
        throw new NotImplementedException();
    }
}