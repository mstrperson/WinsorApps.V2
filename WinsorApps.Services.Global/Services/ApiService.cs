
//#define API_DEBUG

using AsyncAwaitBestPractices;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WinsorApps.Services.Global.Models;


namespace WinsorApps.Services.Global.Services;

public class ApiService : IAsyncInitService, IAutoRefreshingCacheService
{
    private readonly ISavedCredential _credentialManager;

    public TimeSpan RefreshInterval => TimeSpan.FromMinutes(2);
    public bool Refreshing { get; private set; }
    public bool BypassRefreshing { get; private set; }
    public double Progress { get; private set; } = 1;
    public bool Started { get; private set; }

    public bool AutoLoginInProgress { get; private set; } = true;

    private static readonly int MaxConcurrentApiCalls = 100;

    private readonly object _apiCountLock = new();
    private int _openApiCalls = 0;

    private DateTime _lastCallTime = DateTime.Now;
    private DateTime _lastReleaseTime = DateTime.Now;

    private void IncrementApiCount()
    {
        lock(_apiCountLock)
        {
            _openApiCalls++;
        }
        _lastCallTime = DateTime.Now;
    }

    private void DecrementApiCount()
    {
        lock (_apiCountLock)
        {
            _openApiCalls--;
        }
        _lastReleaseTime = DateTime.Now;
    }

    private async Task WaitForApiSpace()
    {
        using DebugTimer _ = new($"Thread {Thread.CurrentThread.ManagedThreadId} waiting for api space", _logging);
        while(_pauseApiCalls)
        {
            await Task.Delay(50);

            if(_lastCallTime - DateTime.Now > TimeSpan.FromSeconds(30))
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, "Releasing Timer for Counting API Threads...");
                _openApiCalls = 0;
                return;
            }
        }
    }

    private bool _pauseApiCalls => _openApiCalls >= MaxConcurrentApiCalls;

    
    public event EventHandler? OnLoginSuccess;
    public event EventHandler? OnCacheRefreshed;

    public bool FirstLogin = true;
    
    public string? AuthUserId => AuthorizedUser?.userId;
    public DateTime? AuthExpires => AuthorizedUser?.expires;

    private AuthResponse? AuthorizedUser { get; set; }
    public UserRecord? UserInfo = default(UserRecord);

    private readonly HttpClient client = new HttpClient()
    {
        BaseAddress =
#if API_DEBUG
            new("http://localhost:5076")
#else
            new("https://forms-dev.winsor.edu")
#endif
    };

    public string BaseAddress => client.BaseAddress!.OriginalString;

    public bool Ready { get; private set; } = false;

    private readonly LocalLoggingService _logging;

    public ApiService(LocalLoggingService localLogging, ISavedCredential credManager)
    {
        _logging = localLogging;
        _credentialManager = credManager;

        RefreshInBackground(CancellationToken.None, err => _logging.LogError(err)).SafeFireAndForget(e => e.LogException(_logging));

    }

    public bool IsMasqing => MasqueradingAdminCred is not null;

    private AuthResponse? MasqueradingAdminCred { get; set; }

    public async Task Refresh(ErrorAction onError)
    {
        await RenewTokenAsync(onError: onError);
        OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public string CacheFileName => ".login.cred";

    public void SaveCache() { }
    public bool LoadCache() => true;

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
    /// Attempts to retrieve saved credentials and if possible, log in to the API.
    /// </summary>
    /// <param name="onErrorAction">If something encounters an error, what do.</param>
    public async Task Initialize(ErrorAction onError)
    {
        Started = true;

        var savedCredential = await _credentialManager.GetSavedCredential();
        if (savedCredential.HasValue)
        {
            AutoLoginInProgress = true;

            CheckForStuckLogin().SafeFireAndForget(e => e.LogException(_logging));
            if (!string.IsNullOrEmpty(savedCredential.Value.JWT) && !string.IsNullOrEmpty(savedCredential.Value.RefreshToken))
            {
                AuthorizedUser = new AuthResponse("", savedCredential.Value.JWT, default, savedCredential.Value.RefreshToken);
                try
                {
                    var success = true;
                    await RenewTokenAsync(onError: err => 
                    {
                        onError(err);
                        success = false;
                    });

                    if (!success && !string.IsNullOrWhiteSpace(savedCredential.Value.SavedPassword))
                    {
                        success = true;
                        await Login(savedCredential.Value.SavedEmail, savedCredential.Value.SavedPassword, onError: err =>
                        {
                            onError(err);
                            success = false;
                        });
                    }

                    if (success)
                        UserInfo = await SendAsync<UserRecord>(HttpMethod.Get, "api/users/self", onError: onError);

                }
                catch (Exception ex)
                {
                    _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to log in automatically",
                        ex.Message);
                    _logging.LogMessage(LocalLoggingService.LogLevel.Debug, ex.StackTrace ?? "No Stack Trace");
                    onError(new("Auto Login Failed", ex.Message));
                    AutoLoginInProgress = false;
                    return;
                }

                AutoLoginInProgress = false;
                return;
            }


            try
            {
                await Login(savedCredential.Value.SavedEmail, savedCredential.Value.SavedPassword, onError);
                _logging.LogMessage(LocalLoggingService.LogLevel.Information,
                    $"Auto Login Successful:  {savedCredential.Value.SavedEmail}");
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to log in automatically", ex.Message);
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, ex.StackTrace ?? "Stack Trace Not Available");
                
                onError(new("Auto Login Failed", ex.Message));
            }
        }
        AutoLoginInProgress = false;
    }

    private async Task CheckForStuckLogin()
    {
        DateTime started = DateTime.Now;

        while(AutoLoginInProgress)
        {
            await Task.Delay(250);

            if(DateTime.Now - started > TimeSpan.FromMinutes(1))
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Auto Login Was Hung for more than 1 minute");
                await _credentialManager.DeleteSavedCredential();
                return;
            }
        }

        _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Auto Login Was Successful...");
    }

    public async Task RefreshInBackground(CancellationToken cancellationToken, ErrorAction onError)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(RefreshInterval);
            if (AuthorizedUser is not null && AuthorizedUser.expires - DateTime.Now < TimeSpan.FromMinutes(5))
            {
                await RenewTokenAsync(onError: onError);
            }
        }
    }

    public async Task DropMasq()
    {
        if (MasqueradingAdminCred is null)
            return;

        AuthorizedUser = MasqueradingAdminCred;
        MasqueradingAdminCred = null;
        UserInfo = await SendAsync<UserRecord>(HttpMethod.Get, "api/users/self");
    }

    public async Task<bool> Masquerade(string masqId, ErrorAction onError)
    {
        var masqToken = await SendAsync<AuthResponse>(HttpMethod.Get, $"api/auth/masq/{masqId}", onError: onError);

        if (masqToken is null)
            return false;
        _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Masquerading as {masqToken.userId}");

        MasqueradingAdminCred = AuthorizedUser;
        AuthorizedUser = masqToken;
        UserInfo = await SendAsync<UserRecord>(HttpMethod.Get, "api/users/self", onError: onError);
        return true;
    }

    public async Task Login(string email, string password, ErrorAction onError)
    {
        using DebugTimer _ = new($"Logging in {email}", _logging);
        Login login = new(email, password);
        try
        {
            AuthorizedUser = await SendAsync<AuthResponse>(HttpMethod.Post, "api/auth",
                JsonSerializer.Serialize(login), false, onError);
            if (AuthorizedUser is not null)
            {

                UserInfo = await SendAsync<UserRecord>(HttpMethod.Get, "api/users/self", onError: onError);
                _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Login Successful:  {email}");
                Ready = true;

                await _credentialManager.Save(email, password, AuthorizedUser.jwt, AuthorizedUser.refreshToken);
                OnLoginSuccess?.Invoke(this, EventArgs.Empty);
                FirstLogin = false;
            }
        }
        catch (Exception e)
        {
            onError.Invoke(new("Login Error", e.Message));
            _logging.LogMessage(LocalLoggingService.LogLevel.Error, $"Login Failed:  {e.Message}");
        }
    }

    public async Task ForgotPassword(string email, string password, Action<string> onCompleteAction, ErrorAction onError)
    {
        using DebugTimer _ = new($"Submitting forgot password for {email}", _logging);
        Login login = new Login(email, password);

        try
        {
            var response = await SendAsync(HttpMethod.Post, "api/auth/forgot",
                JsonSerializer.Serialize(login), false, onError);
            onCompleteAction("Please Check your Email for your new Password.");
        }
        catch (Exception ae)
        {
            onCompleteAction($"Failed: {ae.Message}");
        }
    }

    public async Task Register(string email, string password, Action<string> onCompleteAction, ErrorAction onError)
    {
        using DebugTimer _ = new($"Registering {email}", _logging);
        Login login = new Login(email, password);

        try
        {
            var response = await SendAsync(HttpMethod.Post, "api/auth/register",
                JsonSerializer.Serialize(login), false, onError);
            onCompleteAction(response);
        }
        catch (Exception ae)
        {
            onCompleteAction($"Failed: {ae.Message}");
        }
    }

    public async Task Logout()
    {
        AuthorizedUser = null;
        UserInfo = null;
        await _credentialManager.DeleteSavedCredential();
    }

    public async Task RenewTokenAsync(bool repeat = false, ErrorAction? onError = null)
    {
        using DebugTimer _ = new($"Renewing Login Token", _logging);
        if (Refreshing)
        {
            while(Refreshing)
            {
                await Task.Delay(250);
            }

            return;
        }

        Refreshing = true;
        BypassRefreshing = true;
        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        try
        {
            if (AuthorizedUser is not null)
            {
                var jwt = AuthorizedUser.GetJwt();
                bool failed = false;
                AuthorizedUser = await SendAsync<AuthResponse>(HttpMethod.Get,
                    $"api/auth/renew?refreshToken={AuthorizedUser.refreshToken}", authorize: true, onError: err =>
                    {
                        _logging.LogError(err);
                        failed = true;
                    });

                if (failed)
                {
                    var saved = await _credentialManager.GetSavedCredential();
                    if(saved.HasValue && !string.IsNullOrWhiteSpace(saved.Value.SavedPassword))
                    {
                        var savedCred = saved.Value;
                        failed = false;
                        await Login(savedCred.SavedEmail, savedCred.SavedPassword, onError: err =>
                        {
                            _logging.LogError(err);
                            failed = true;
                        });
                    }

                    if(failed)
                        Refreshing = false;
                    
                }
                if(AuthorizedUser is null)
                {
                    Refreshing = false;
                    _logging.LogMessage(LocalLoggingService.LogLevel.Error, $"Renewing Token returned a Null result, but did not trigger error.... This should not be possible.");
                    
                }

                if (!failed && AuthorizedUser is not null)
                {
                    _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Renewed security token.");
                    await _credentialManager.SaveJwt(AuthorizedUser.jwt, AuthorizedUser.refreshToken);
                }
            }
            else
            {
                var savedCred = await _credentialManager.GetSavedCredential();
                if (!savedCred.HasValue)
                    throw new InvalidOperationException("Cannot renew a token without logging in first.");

                if (!repeat && !string.IsNullOrEmpty(savedCred.Value.JWT) && !string.IsNullOrEmpty(savedCred.Value.RefreshToken))
                {
                    AuthorizedUser = new(jwt: savedCred.Value.JWT, refreshToken: savedCred.Value.RefreshToken);
                }
                if (!string.IsNullOrWhiteSpace(savedCred?.SavedPassword))
                    await Login(savedCred.Value.SavedEmail, savedCred.Value.SavedPassword, onError);
                else
                {
                    Refreshing = false;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ex?.LogException(_logging);
        }

        Refreshing = false;

        if (AuthorizedUser is not null)
        {
            if (UserInfo is null)
                UserInfo = await SendAsync<UserRecord>(HttpMethod.Get, "api/users/self");
            _logging.LogMessage(LocalLoggingService.LogLevel.Information,
                $"Login Successful:  {UserInfo.Value.email}");
            Ready = true; 
            
            if (FirstLogin)
            {
                FirstLogin = false;
                OnLoginSuccess?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private async Task<HttpRequestMessage> BuildRequest(HttpMethod method, string endpoint, string jsonContent = "",
        bool authorize = true, FileStreamWrapper? streamContent = null)
    {
        HttpRequestMessage request = new HttpRequestMessage(method, endpoint);

        if(authorize && Refreshing && !BypassRefreshing)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Token is being renewed right now, so I have to wait... Thread: {Thread.CurrentThread.ManagedThreadId}");
            while(Refreshing)
            {
                await Task.Delay(100);
            }
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Awaited Token Refresh complete Thread: {Thread.CurrentThread.ManagedThreadId}");
        }

        BypassRefreshing = false;

        if (authorize && (AuthorizedUser is null))
        {
            throw new UnauthorizedAccessException("Unable to Authorize request.  Token is missing or expired.");
        }

        if (authorize)
        {
            var authHeader = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthorizedUser!.jwt);
            request.Headers.Authorization = authHeader;
        }

        if (!string.IsNullOrEmpty(jsonContent))
        {
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }
        else if (streamContent is not null)
        {
            var content = new StreamContent(streamContent.contentStream);
            content.Headers.ContentType = new(streamContent.contentType);

            request.Content = new MultipartFormDataContent
            {
                {content, "file", streamContent.fileName}
            };
        }


        return request;
    }

    public async Task<string> SendAsync(HttpMethod method, string endpoint, string jsonContent = "",
        bool authorize = true,
        ErrorAction? onError = null, bool isReAuth = false,
        FileStreamWrapper? stream = null)
    {
        await WaitForApiSpace();

        using DebugTimer _ = new($"Send Async to {endpoint} with jsonContent {jsonContent}", _logging);

        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        var request = await BuildRequest(method, endpoint, jsonContent, authorize, stream);

        IncrementApiCount();
        var response = await client.SendAsync(request);
        DecrementApiCount();
        if (await CheckReAuth(response, () => BuildRequest(method, endpoint, jsonContent, authorize, stream).Result))
        {
            onError(new("Unauthorized Access", "Current user is not authorized to access this endpoint."));
            return "";
        }

        if (!response.IsSuccessStatusCode)
        {
            await ProcessHttpResponse(response, onError);
            return "";
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.Accepted)
        {
            
            return "";
        }
        var message = await response.Content.ReadAsStringAsync();
        DecrementApiCount();
        return message;
    }

    public async Task<DocumentHeader?> UploadDocument(string endpoint, DocumentHeader header, byte[] fileContent, ErrorAction onError, bool isReAuth = false)
    {
        await WaitForApiSpace();
        using DebugTimer _ = new($"Uploading document to {endpoint} with content {header.fileName} [{fileContent.Length} bytes]", _logging);
        using var request = await BuildRequest(HttpMethod.Post, endpoint);
        using MemoryStream ms = new(fileContent);
        var content = new MultipartFormDataContent
        {
            {
                new StreamContent(ms),
                "file",
                header.fileName
            }
        };

        IncrementApiCount();
        var response = await client.SendAsync(request);
        DecrementApiCount();

        if (!response.IsSuccessStatusCode)
        {
            if (!isReAuth && response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                AuthorizedUser is not null)
            {
                await RenewTokenAsync(onError: onError);
                return await UploadDocument(endpoint, header, fileContent, onError, true);
            }
        }

        return await ProcessHttpResponse<DocumentHeader>(response, onError);
    }

    public async Task<Stream> DownloadStream(string endpoint, string jsonContent = "", bool authorize = true,
        ErrorAction? onError = null, bool isReAuth = false, FileStreamWrapper? stream = null)
    {
        await WaitForApiSpace();
        using DebugTimer _ = new($"Downloading stream from {endpoint} with jsonContent {jsonContent}", _logging);
        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        var request = await BuildRequest(HttpMethod.Get, endpoint, jsonContent, authorize, stream);
        IncrementApiCount();
        var response = await client.SendAsync(request);
        DecrementApiCount();
        if (!response.IsSuccessStatusCode)
        {
            if (!isReAuth && response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                AuthorizedUser is not null)
            {
                await RenewTokenAsync(onError: onError);
                return await DownloadStream(endpoint, jsonContent, authorize, onError, true);
            }

            await ProcessHttpResponse(response, onError);
            return new MemoryStream();
        }

        return await response.Content.ReadAsStreamAsync();

    }



    public async Task<FileStreamWrapper?> DownloadFileExt(string endpoint, string jsonContent = "",
        bool authorize = true,
        ErrorAction? onError = null, bool isReAuth = false, FileStreamWrapper? stream = null)
    {
        await WaitForApiSpace();
        using DebugTimer _ = new($"Downloading File to {endpoint} with jsonContent {jsonContent}", _logging);
        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        var request = await BuildRequest(HttpMethod.Get, endpoint, jsonContent, authorize, stream);
        IncrementApiCount();
        var response = await client.SendAsync(request);
        DecrementApiCount();
        if (!response.IsSuccessStatusCode)
        {
            if (!isReAuth && response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                AuthorizedUser is not null)
            {
                await RenewTokenAsync();
                return await DownloadFileExt(endpoint, jsonContent, authorize, onError, true);
            }

            await ProcessHttpResponse(response, onError);
            return null;
        }

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? string.Empty;
        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        return new(await response.Content.ReadAsStreamAsync(), contentType, fileName);

    }

    public async Task<byte[]> DownloadFile(string endpoint, string jsonContent = "", bool authorize = true,
        ErrorAction? onError = null, bool isReAuth = false,
        FileStreamWrapper? inStream = null)
    {
        await WaitForApiSpace();
        using DebugTimer _ = new($"Downloading file to {endpoint} with jsonContent {jsonContent}", _logging);
        onError ??= _logging.LogError;
        var request = await BuildRequest(
            (string.IsNullOrEmpty(jsonContent) && inStream is null) ? HttpMethod.Get : HttpMethod.Post,
            endpoint, jsonContent, authorize, inStream);

        IncrementApiCount();
        var response = await client.SendAsync(request);
        DecrementApiCount();
        if (!response.IsSuccessStatusCode)
        {
            if (!isReAuth && response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                AuthorizedUser is not null)
            {
                await RenewTokenAsync();
                return await DownloadFile(endpoint, jsonContent, authorize, onError, true);
            }

            await ProcessHttpResponse(response, onError);
            return [];
        }

        using MemoryStream ms = new();
        var stream = await response.Content.ReadAsStreamAsync();

        await stream.CopyToAsync(ms);
        return ms.ToArray();

    }

    public async Task<TOut?> SendAsync<TIn, TOut>(HttpMethod method, string endpoint, TIn content,
        bool authorize = true, ErrorAction? onError = null)
    {
        var json = JsonSerializer.Serialize(content);
        return await SendAsync<TOut>(method, endpoint, json, authorize, onError);
    }

    public async Task<T?> SendAsync<T>(HttpMethod method, string endpoint, string jsonContent = "",
        bool authorize = true, ErrorAction? onError = null, bool isReAuth = false)
    {
        await WaitForApiSpace();
        using DebugTimer _ = new($"Send Async to {endpoint} with jsonContent {jsonContent}", _logging);
        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        var request = await BuildRequest(method, endpoint, jsonContent, authorize);
        IncrementApiCount();
        var response = await client.SendAsync(request);
        DecrementApiCount();
        if (endpoint!="api/auth" && await CheckReAuth(response, () => BuildRequest(method, endpoint, jsonContent, authorize).Result))
        {
            onError(new("Unauthorized Access", "Current user is not authorized to access this endpoint."));
            var result = await response.Content.ReadAsStringAsync();
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Call to {method} {endpoint} failed to authorize properly.", result);
            return default;
        }
        
        return await ProcessHttpResponse<T>(response, onError);
    }

    private async Task<T?> ProcessHttpResponse<T>(HttpResponseMessage response, ErrorAction onError, T? defaultOutput = default)
    {
        var jsonContent = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(jsonContent);
                return result;
            }
            catch (Exception e)
            {
                e.LogException(_logging);
                onError(new("Failed To Deserialize Result", e.Message));
                return defaultOutput;
            }
        }

        try
        {
            var err = JsonSerializer.Deserialize<ErrorRecord>(jsonContent);
            onError(err);
        }
        catch (Exception e)
        {
            e.LogException(_logging);
            onError(new($"{response.ReasonPhrase}", jsonContent));
        }
        
        return defaultOutput;
    }

    /// <summary>
    /// Returns TRUE if renewing the Token fails to get passed Unauthorized.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="requestBuilder"></param>
    /// <returns></returns>
    public async Task<bool> CheckReAuth(HttpResponseMessage response, Func<HttpRequestMessage> requestBuilder)
    {
        for (var i = 0; i < 5; i++)
        {
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return false;
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"Request to {response.RequestMessage?.RequestUri?.PathAndQuery ?? "unknown uri"} returned Unauthorized. Retry Count: {i}");
            await RenewTokenAsync();
            response = await client.SendAsync(requestBuilder());
        }

        return true;
    }
    
    private async Task<bool> ProcessHttpResponse(HttpResponseMessage response, ErrorAction onError)
    {
        if (response.IsSuccessStatusCode) 
            return true;
        var jsonContent = await response.Content.ReadAsStringAsync();
        
        try
        {
            var err = JsonSerializer.Deserialize<ErrorRecord>(jsonContent);
            onError(err);
        }
        catch (Exception e)
        {
            e.LogException(_logging);
            onError(new($"{response.ReasonPhrase}", jsonContent));
        }

        return false;
    }
}

