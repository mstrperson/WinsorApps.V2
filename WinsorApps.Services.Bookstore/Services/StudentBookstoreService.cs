using System.Collections.Immutable;
using WinsorApps.Services.Bookstore.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Bookstore.Services;

public class StudentBookstoreService(ApiService api, LocalLoggingService logging, RegistrarService registrar) : IAsyncInitService
{
    private readonly ApiService _api = api;
    private readonly LocalLoggingService _logging = logging;
    private readonly RegistrarService _registrar = registrar;

    public StudentSemesterBookList FallBookList { get; private set; } = new("fall", []);
    public StudentSemesterBookList SpringBookList { get; private set; } = new("spring", []);

    public List<OrderStatus> OrderStatusOptions { get; private set; } = [];

    private List<StudentSectionBookOrder> _myOrders = [];

    /// <summary>
    /// Populated during Initialization, automatically updated when Student Orders are created
    /// </summary>
    public Dictionary<string, StudentSectionBookOrder> BookOrdersBySection { get; } = [];

    /// <summary>
    /// Set to true once the Initialization method has completed.
    /// </summary>
    public bool Ready { get; private set; } = false;

    public bool Started { get; private set; }

    public double Progress { get; private set; }

    public string CacheFileName => throw new NotImplementedException();

    /// <summary>
    /// Get Data from Cache. This data is populated during initialization and does not need to be refreshed (theoretically...)
    /// </summary>
    /// <param name="fall">ideally, you can calculate this based on the current DateTime.Today for default behavior,
    /// but an end user can request either term whenever they wish.</param>
    /// <returns></returns>
    /// <exception cref="ServiceNotReadyException">If the service is still being initialized.</exception>
    public StudentSemesterBookList MyBookList(bool fall = true)
    {
        if (!Ready)
            throw new ServiceNotReadyException(_logging, "Student Bookstore Service is not ready yet.");

        return fall ? FallBookList : SpringBookList;
    }

    /// <summary>
    /// Call this method Once after the user has logged in successfully.  
    /// Ideally this will be in the OnLoginComplete event handler on the MainPage.
    /// </summary>
    /// <param name="onError"></param>
    /// <returns></returns>
    public async Task Initialize(ErrorAction onError)
    {
        while (!_registrar.Ready)
        {
            await Task.Delay(250);
        }

        var mySchedule = await _registrar.GetMyAcademicScheduleAsync();

        var statusTask = _api.SendAsync<List<OrderStatus>>(HttpMethod.Get,
            "api/book-orders/students/status-list", onError: onError);
        statusTask.WhenCompleted(() => { OrderStatusOptions = statusTask.Result ?? []; });

        var fallOrderTask = GetSemesterBookList(true, onError);
        var springOrderTask = GetSemesterBookList(false, onError);
        var myOrdersTask = GetMyOrders(onError);

        List<Task> taskList = [fallOrderTask, springOrderTask, myOrdersTask];
        foreach (var section in mySchedule)
        {
            taskList.Add(GetMyOrdersFor(section.sectionId, onError));
        }

        await Task.WhenAll(taskList);



        Ready = true;
    }

    /// <summary>
    /// This will pull your book list directly from the Server rather than from your local cache,
    /// this method is invoked during initialization to provide the cached values.  So, you shouldn't
    /// need to call this method manually. (But you can if you want to Refresh the cache!)
    /// </summary>
    /// <param name="fall"></param>
    /// <param name="onError"></param>
    /// <returns></returns>
    public async Task<StudentSemesterBookList> GetSemesterBookList(bool fall, ErrorAction onError)
    {
        string param = fall ? "fall" : "spring";
        var result = await _api.SendAsync<StudentSemesterBookList?>(HttpMethod.Get,
            $"api/book-orders/students/book-list?term={param}",
            onError: onError);

        result ??= new(param, []);

        if (fall)
            FallBookList = result;
        else
            SpringBookList = result;

        return result;
    }

    /// <summary>
    /// This method gets you current book orders from the server.  It is invoked
    /// during initialization to fill the cache, but may be updated later if needed.
    /// (Theoretically you shouldn't need to, but it is available~)
    /// </summary>
    /// <param name="onError"></param>
    /// <returns></returns>
    public async Task<List<StudentSectionBookOrder>> GetMyOrders(ErrorAction onError)
    {
        var result = await _api.SendAsync<List<StudentSectionBookOrder>?>(HttpMethod.Get,
            $"api/book-orders/students/orders",
            onError: onError);

        result ??= [];

        _myOrders = [..result];
        return _myOrders;
    }

    /// <summary>
    /// Pull the Orders for a specific section from the server.  This is invoked 
    /// for all classes in your academic schedule during initialization. You may
    /// however, choose to update manually at any time.
    /// </summary>
    /// <param name="sectionId"></param>
    /// <param name="onError"></param>
    /// <returns></returns>
    public async Task<StudentSectionBookOrder?> GetMyOrdersFor(string sectionId, ErrorAction onError)
    {
        var result = await _api.SendAsync<StudentSectionBookOrder?>(HttpMethod.Get,
            $"api/book-orders/students/orders/{sectionId}",
            onError: onError);

        if (result is not null)
            BookOrdersBySection[sectionId] = result;

        return result;
    }

    /// <summary>
    /// This is the main active method that you'll need to use in this application.
    /// This will create or update a book order for students.  To "Delete" an order entirely,
    /// pass in [] for the selected ISBN list.
    /// </summary>
    /// <param name="sectionId">the section.id of a class on your schedule.</param>
    /// <param name="selectedIsbns">All the ISBNs that you are currently looking to order.  This may be an empty list.</param>
    /// <param name="onError"></param>
    /// <returns></returns>
    public async Task<StudentSectionBookOrder?> CreateOrUpdateBookOrder(string sectionId,
        IEnumerable<string> selectedIsbns, ErrorAction onError)
    {
        var result = await _api.SendAsync<IEnumerable<string>, StudentSectionBookOrder?>(
            HttpMethod.Post,
            $"api/book-orders/students/orders/{sectionId}",
            selectedIsbns,
            onError: onError);

        if (result is null)
            return result;

        BookOrdersBySection[sectionId] = result;
        if (_myOrders.Any(ord => ord.sectionId == sectionId))
        {
            var oldOrder = _myOrders!.First(ord => ord.sectionId == sectionId)!;
            _myOrders.Replace(oldOrder, result);
        }
        else
            _myOrders.Add(result);

        return result;
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }

    public async Task Refresh(ErrorAction onError)
    {
        var mySchedule = await _registrar.GetMyAcademicScheduleAsync();

        var statusTask = _api.SendAsync<List<OrderStatus>>(HttpMethod.Get,
            "api/book-orders/students/status-list", onError: onError);
        statusTask.WhenCompleted(() => { OrderStatusOptions = statusTask.Result ?? []; });

        var fallOrderTask = GetSemesterBookList(true, onError);
        var springOrderTask = GetSemesterBookList(false, onError);
        var myOrdersTask = GetMyOrders(onError);

        List<Task> taskList = [fallOrderTask, springOrderTask, myOrdersTask];
        foreach (var section in mySchedule)
        {
            taskList.Add(GetMyOrdersFor(section.sectionId, onError));
        }

        await Task.WhenAll(taskList);

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