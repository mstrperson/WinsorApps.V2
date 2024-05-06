
//#define API_DEBUG

using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using WinsorApps.Services.Global.Models;


namespace WinsorApps.Services.Global.Services;

public class ApiService : IAsyncInitService, IAutoRefreshingService
{
    public TimeSpan RefreshInterval => TimeSpan.FromMinutes(45);
    public bool Refreshing { get; private set; }
    public double Progress => 1;
    public bool Started { get; private set; }

public event EventHandler? OnLoginSuccess;

    public bool FirstLogin = true;
    
    public string? AuthUserId => AuthorizedUser?.userId;
    public DateTime? AuthExpires => AuthorizedUser?.expires;

    private AuthResponse? AuthorizedUser { get; set; }
    public UserRecord? UserInfo;

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

    public ApiService(LocalLoggingService localLogging)
    {
        _logging = localLogging;
    }

    public AuthResponse? MasqueradingAdminCred { get; private set; }

    public async Task Refresh(ErrorAction onError)
    {
        await RenewTokenAsync(onError: onError);
    }

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
        var savedCredential = await SavedCredential.GetSavedCredential();
        if (savedCredential is not null)
        {
            if (!string.IsNullOrEmpty(savedCredential.JWT) && !string.IsNullOrEmpty(savedCredential.RefreshToken))
            {
                AuthorizedUser = new AuthResponse("", savedCredential.JWT, default, savedCredential.RefreshToken);
                try
                {
                    await RenewTokenAsync(onError: onError);
                }
                catch (Exception ex)
                {
                    _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to log in automatically",
                        ex.Message);
                    _logging.LogMessage(LocalLoggingService.LogLevel.Debug, ex.StackTrace);
                    onError(new("Auto Login Failed", ex.Message));
                    return;
                }

                return;
            }


            try
            {
                await Login(savedCredential.SavedEmail, savedCredential.SavedPassword, onError);
                _logging.LogMessage(LocalLoggingService.LogLevel.Information,
                    $"Auto Login Successful:  {savedCredential.SavedEmail}");
            }
            catch (Exception ex)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to log in automatically", ex.Message);
                _logging.LogMessage(LocalLoggingService.LogLevel.Debug, ex.StackTrace ?? "Stack Trace Not Available");
                
                onError(new("Auto Login Failed", ex.Message));
            }
        }

    }

    public async Task RefreshInBackground(CancellationToken cancellationToken, ErrorAction onError)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(RefreshInterval);
            if (AuthorizedUser is not null && AuthorizedUser.expires < DateTime.Now.AddMinutes(-2))
            {
                Refreshing = true;
                await RenewTokenAsync(onError: onError);
                Refreshing = false;
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
                SavedCredential.SaveJwt(AuthorizedUser.jwt, AuthorizedUser.refreshToken);
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
        Login login = new Login(email, password);

        try
        {
            var response = await SendAsync(HttpMethod.Post, "api/auth/forgot",
                JsonSerializer.Serialize(login), false, onError);
            onCompleteAction(response);
        }
        catch (Exception ae)
        {
            onCompleteAction($"Failed: {ae.Message}");
        }
    }

    public async Task Register(string email, string password, Action<string> onCompleteAction, ErrorAction onError)
    {
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

    public void Logout()
    {
        AuthorizedUser = null;
        UserInfo = null;
        SavedCredential.DeleteSavedCredential();
    }

    public async Task RenewTokenAsync(bool repeat = false, ErrorAction? onError = null)
    {
        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        try
        {
            if (AuthorizedUser is not null)
            {
                var jwt = AuthorizedUser.GetJwt();
                AuthorizedUser = await SendAsync<AuthResponse>(HttpMethod.Get,
                    $"api/auth/renew?refreshToken={AuthorizedUser.refreshToken}", authorize: true);
                _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Renewed security token.");
                if (UserInfo is null)
                    UserInfo = await SendAsync<UserRecord>(HttpMethod.Get, "api/users/self");
                _logging.LogMessage(LocalLoggingService.LogLevel.Information,
                    $"Login Successful:  {UserInfo.Value.email}");
                Ready = true;
                if(FirstLogin)
                {
                    OnLoginSuccess?.Invoke(this, EventArgs.Empty);
                    FirstLogin = false;
                }
                SavedCredential.SaveJwt(AuthorizedUser.jwt, AuthorizedUser.refreshToken);
            }
            else
            {
                var savedCred = await SavedCredential.GetSavedCredential();
                if (savedCred is null)
                    throw new InvalidOperationException("Cannot renew a token without logging in first.");

                if (!repeat && !string.IsNullOrEmpty(savedCred.JWT) && !string.IsNullOrEmpty(savedCred.RefreshToken))
                {
                    AuthorizedUser = new(jwt: savedCred.JWT, refreshToken: savedCred.RefreshToken);
                }

                await Login(savedCred.SavedEmail, savedCred.SavedPassword, onError);
            }
        }
        catch (Exception ex)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to Renew Token", ex.Message);
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, ex.StackTrace);
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string endpoint, string jsonContent = "",
        bool authorize = true, FileStreamWrapper? streamContent = null)
    {
        HttpRequestMessage request = new HttpRequestMessage(method, endpoint);
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

    /*
    public async Task SendAsync(HttpMethod method, string endpoint, string jsonContent = "", bool authorize = true,
        ErrorAction? onError = null, bool isReAuth = false, FileStreamWrapper? stream = null)
    {
        var request = BuildRequest(method, endpoint, jsonContent, authorize, stream);

        try
        {
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                if (!isReAuth && response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    AuthorizedUser is not null)
                {
                    await RenewTokenAsync();
                    await SendAsync(method, endpoint, jsonContent, authorize, onNonSuccessStatus, true);
                    return;
                }

                if (onNonSuccessStatus is null)
                    throw new ApiException(response, _logging);

                onNonSuccessStatus(response);
            }
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"ApiService - SendAsync<T>:  {e.Message}",
                e.StackTrace ?? "StackTrace not available.");
            throw new InvalidOperationException($"Api Call to {endpoint} failed.", e);
        }

    }
*/
    public async Task<string> SendAsync(HttpMethod method, string endpoint, string jsonContent = "",
        bool authorize = true,
        ErrorAction? onError = null, bool isReAuth = false,
        FileStreamWrapper? stream = null)
    {
        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        var request = BuildRequest(method, endpoint, jsonContent, authorize, stream);

        var response = await client.SendAsync(request);
        if (await CheckReAuth(response, () => BuildRequest(method, endpoint, jsonContent, authorize, stream)))
        {
            onError(new("Unauthorized Access", "Current user is not authorized to access this endpoint."));
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            await ProcessHttpResponse(response, onError);
            return "";
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.Accepted)
            return "";

        var message = await response.Content.ReadAsStringAsync();

        return message;
    }

    public async Task<Stream> DownloadStream(string endpoint, string jsonContent = "", bool authorize = true,
        ErrorAction? onError = null, bool isReAuth = false, FileStreamWrapper? stream = null)
    {
        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        var request = BuildRequest(HttpMethod.Get, endpoint, jsonContent, authorize, stream);
        var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                if (!isReAuth && response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    AuthorizedUser is not null)
                {
                    await RenewTokenAsync();
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
        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        var request = BuildRequest(HttpMethod.Get, endpoint, jsonContent, authorize, stream);
         var response = await client.SendAsync(request);
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
        var request = BuildRequest(
            (string.IsNullOrEmpty(jsonContent) && inStream is null) ? HttpMethod.Get : HttpMethod.Post,
            endpoint, jsonContent, authorize, inStream);
        
            var response = await client.SendAsync(request);
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
        onError ??= err => _logging.LogMessage(LocalLoggingService.LogLevel.Error, err.error);
        var request = BuildRequest(method, endpoint, jsonContent, authorize);
        var response = await client.SendAsync(request);
        if (await CheckReAuth(response, () => BuildRequest(method, endpoint, jsonContent, authorize)))
        {
            onError(new("Unauthorized Access", "Current user is not authorized to access this endpoint."));
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

    public async Task<bool> CheckReAuth(HttpResponseMessage response, Func<HttpRequestMessage> requestBuilder)
    {
        for (var i = 0; i < 5; i++)
        {
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return false;
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

