using System.Diagnostics;
using System.Text.Json;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.Services.Helpdesk.Services;

public class ServiceCaseService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;
    public bool Ready { get; private set; } = false;

    private ImmutableArray<ServiceStatus>? _serviceStatuses;

    public ImmutableArray<ServiceStatus> ServiceStatuses
    {
        get
        {
            if (!Ready)
                throw new ServiceNotReadyException(_logging, "Device Service has not retrieved ServiceStatuses yet.");

            return _serviceStatuses ?? Enumerable.Empty<ServiceStatus>().ToImmutableArray();
        }
    }

    private ImmutableArray<ServiceCaseCommonIssue>? _commonIssues;

    public ImmutableArray<ServiceCaseCommonIssue> CommonIssues
    {
        get
        {
            if (!Ready)
                throw new ServiceNotReadyException(_logging, "Device Service has not retrieved CommonIssues yet.");

            return _commonIssues ?? Enumerable.Empty<ServiceCaseCommonIssue>().ToImmutableArray();
        }
    }

    private List<ServiceCase> _openCasesCache;

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
            return _openCasesCache?.ToDictionary(c => c.id) ?? new Dictionary<string, ServiceCase>();
        }
    }

    public ImmutableArray<ServiceCase> OpenCases =>
        Ready ? _openCasesCache.ToImmutableArray() : Array.Empty<ServiceCase>().ToImmutableArray();

    public async Task Initialize(ErrorAction onError)
    {
        _serviceStatuses = await _api.SendAsync<ImmutableArray<ServiceStatus>>(
            HttpMethod.Get, "api/helpdesk/service-cases/status-list", onError: onError);
        _commonIssues = await _api.SendAsync<ImmutableArray<ServiceCaseCommonIssue>>(
            HttpMethod.Get, "api/helpdesk/service-cases/common-issue-list", onError: onError);

        _openCasesCache = (await _api.SendAsync<List<ServiceCase>>(
            HttpMethod.Get, "api/helpdesk/service-cases", onError: onError))!;

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
        var result = await _api.SendAsync<NewServiceCase, ServiceCase>(
            HttpMethod.Post, "api/helpdesk/service-cases/create", serviceCase, onError: onError);

        if (result is null)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug,
                "Create Service Case endpoint returned a null result.", $"{serviceCase}");
            return null;
        }

        _logging.LogMessage(LocalLoggingService.LogLevel.Information,
            $"Created new Service Case for {result.device.serialNumber}");
        _openCasesCache.Add(result);
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

        var status = ServiceStatuses.First(st => st.text == result.status);
        if (status.isClosed && oldCase is not null)
        {
            _openCasesCache.Remove(oldCase);
            return true;
        }

        if (oldCase is not null)
            _openCasesCache.Replace(oldCase, result);
        else
            _openCasesCache.Add(result);

        return true;
    }

    public async Task<bool> CloseServiceCase(string caseId,
        ErrorAction onError, ServiceStatus? closingStatus = null)
    {
        var endpoint = $"api/helpdesk/service-cases/{caseId}/close";
        if (closingStatus is not null)
            endpoint += $"?closingStatus={closingStatus.Value.id}";
        var result = await _api.SendAsync<ServiceCase>(HttpMethod.Put, endpoint, onError: onError);

        if (result is null)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Closing Service Case failed.");
            return false;
        }

        var cachedCase = _openCasesCache.FirstOrDefault(c => c.id == caseId);
        if (cachedCase is not null)
        {
            _openCasesCache.Remove(cachedCase);
        }

        return true;
    }
}