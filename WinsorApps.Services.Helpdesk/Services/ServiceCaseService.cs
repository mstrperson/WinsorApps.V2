using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.Services.Helpdesk.Services;

public class ServiceCaseService : IAsyncInitService, IAutoRefreshingCacheService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    public bool Ready { get; private set; } = false;
    public double Progress { get; private set; } = 0;

    private ImmutableArray<ServiceStatus>? _serviceStatuses;
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
    public ImmutableArray<ServiceStatus> ServiceStatuses
    {
        get
        {
            if (!Ready)
                throw new ServiceNotReadyException(_logging, "Device Service has not retrieved ServiceStatuses yet.");

            return _serviceStatuses ?? [];
        }
    }

    private ImmutableArray<ServiceCaseCommonIssue>? _commonIssues;

    public ImmutableArray<ServiceCaseCommonIssue> CommonIssues
    {
        get
        {
            if (!Ready)
                throw new ServiceNotReadyException(_logging, "Device Service has not retrieved CommonIssues yet.");

            return _commonIssues ?? [];
        }
    }

    private List<ServiceCase> _openCasesCache = [];

    public event EventHandler? OnCacheRefreshed;

    public ServiceCaseService(ApiService api, LocalLoggingService logging)
    {
        _api = api;
        _logging = logging;
    }

    public Dictionary<string, ServiceCase> OpenCasesCacheById
    {
        get
        {
            if (!Ready)
                throw new ServiceNotReadyException(_logging, "Device Service has not populated the Service Case Cache yet.");
            return _openCasesCache.ToDictionary(c => c.id) ?? new Dictionary<string, ServiceCase>();
        }
    }

    public ImmutableArray<ServiceCase> OpenCases =>
        Ready ? [.. _openCasesCache] : [];

    public bool Started { get; private set; }

    public TimeSpan RefreshInterval => TimeSpan.FromMinutes(30);

    public bool Refreshing { get; private set; }

    public string CacheFileName => throw new NotImplementedException();

    public async Task Refresh(ErrorAction onError)
    {
        Refreshing = true;
        _openCasesCache = (await _api.SendAsync<List<ServiceCase>>(
            HttpMethod.Get, "api/helpdesk/service-cases?open=true", onError: onError))!;
        Refreshing = false;
    }
    
    public async Task<ImmutableArray<ServiceCase>> SearchServiceCaseHistory(ServiceCaseFilter filter, ErrorAction onError)
    {
        return await _api.SendAsync<ImmutableArray<ServiceCase>>(HttpMethod.Get,
            $"api/helpdesk/service-cases?{filter.QueryString}", onError: onError);
    }

    public async Task Initialize(ErrorAction onError)
    {
        if (Started)
            return;

        Started = true;
        _serviceStatuses = await _api.SendAsync<ImmutableArray<ServiceStatus>>(
            HttpMethod.Get, "api/helpdesk/service-cases/status-list", onError: onError);
        Progress = 1 / 3.0;
        _commonIssues = await _api.SendAsync<ImmutableArray<ServiceCaseCommonIssue>>(
            HttpMethod.Get, "api/helpdesk/service-cases/common-issue-list", onError: onError);

        Progress = 2 / 3.0;
        _openCasesCache = (await _api.SendAsync<List<ServiceCase>>(
            HttpMethod.Get, "api/helpdesk/service-cases?open=true", onError: onError))!;

        Progress = 1;
        Ready = true;
    }

    public async Task UpdateServiceCaseCache(ErrorAction onError)
    {
        var result = await _api.SendAsync<List<ServiceCase>>(
            HttpMethod.Get, "api/helpdesk/service-cases", onError: onError);
        if (result is not null)
            _openCasesCache = result;
    }

    public async Task<ServiceCase?> OpenNewServiceCaseAsync(NewServiceCase serviceCase,
        ErrorAction onError)
    {
        var result = await _api.SendAsync<NewServiceCase, ServiceCase?>(
            HttpMethod.Post, "api/helpdesk/service-cases/create", serviceCase, onError: onError);

        if (!result.HasValue)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug,
                "Create Service Case endpoint returned a null result.", $"{serviceCase}");
            return null;
        }

        _logging.LogMessage(LocalLoggingService.LogLevel.Information,
            $"Created new Service Case for {result.Value.device.serialNumber}");
        _openCasesCache.Add(result.Value);
        return result;
    }

    public async Task<bool> SendPickupNotification(string caseId, ErrorAction onError)
    {
        bool success = true;
        await _api.SendAsync(HttpMethod.Get, $"api/helpdesk/service-cases/{caseId}/notify", onError: err =>
        {
            success = false;
            onError(err);
        });

        return success;
    }

    public async Task<bool> AssignLoanerToCase(string caseId, string assetTag,
        ErrorAction onError)
    {
        var result = await _api.SendAsync<ServiceCase>(
            HttpMethod.Put, $"api/helpdesk/service-cases/{caseId}/loaner/{assetTag}", onError: onError);

        return HandleServiceCaseResult(caseId, result);
    }

    public async Task<byte[]> PrintSticker(string caseId, ErrorAction? onError)
    {
        var result = await _api.DownloadFile($"api/helpdesk/service-cases/{caseId}/sticker", onError: onError);

        return result;
    }


    public async Task<ServiceCase?> UpdateServiceCase(UpdateServiceCase update,
        ErrorAction? onError)
    {
        var result = await _api.SendAsync<UpdateServiceCase, ServiceCase>(
            HttpMethod.Put, "api/helpdesk/service-cases/update", update, onError: onError);

        HandleServiceCaseResult(update.caseId, result!);
        return result;
    }

    public async Task<bool> IncrementCaseStatus(string caseId, ErrorAction onError)
    {
        var result = await _api.SendAsync<ServiceCase>(
            HttpMethod.Get, $"api/helpdesk/service-cases/{caseId}/increment-status", onError: onError);
        return HandleServiceCaseResult(caseId, result);
    }

    private bool HandleServiceCaseResult(string caseId, ServiceCase? result)
    {
        if (result is null)
            return false;

        var oldCase = _openCasesCache.FirstOrDefault(c => c.id == caseId);

        var status = ServiceStatuses.First(st => st.text == result.Value.status);
        if (status.isClosed && oldCase != default)
        {
            _openCasesCache.Remove(oldCase);
            return true;
        }

        if (oldCase != default)
            _openCasesCache.Replace(oldCase, result.Value);
        else
            _openCasesCache.Add(result.Value);

        return true;
    }

    public async Task<bool> CloseServiceCase(string caseId,
        ErrorAction onError, ServiceStatus? closingStatus = null)
    {
        var endpoint = $"api/helpdesk/service-cases/{caseId}/close";
        if (closingStatus is not null)
            endpoint += $"?closingStatus={closingStatus.Value.id}";
        var result = await _api.SendAsync<ServiceCase?>(HttpMethod.Put, endpoint, onError: onError);

        if (!result.HasValue)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Closing Service Case failed.");
            return false;
        }

        var cachedCase = _openCasesCache.FirstOrDefault(c => c.id == caseId);
        if (cachedCase != default)
        {
            _openCasesCache.Remove(cachedCase);
        }

        return true;
    }

    public async Task RefreshInBackground(CancellationToken token, ErrorAction onError)
    {
        while(!token.IsCancellationRequested)
        {
            await Task.Delay(RefreshInterval, token);
        }
    }

    public async Task SaveCache()
    {
        throw new NotImplementedException();
    }

    public bool LoadCache()
    {
        throw new NotImplementedException();
    }
}