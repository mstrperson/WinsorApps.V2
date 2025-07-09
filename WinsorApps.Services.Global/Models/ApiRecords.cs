using System.Collections.Immutable;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text.Json;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Global.Models;

public sealed class ApiException : Exception
    {
        public ErrorRecord ErrorRecord { get; private set; }

        public ApiException(HttpResponseMessage responseMessage, LocalLoggingService loggingService) : base(responseMessage.Content.ReadAsStringAsync().Result)
        {
            try
            {
                var json = responseMessage.Content.ReadAsStringAsync().Result;
                ErrorRecord = JsonSerializer.Deserialize<ErrorRecord>(json) ?? new("Content was not an Error Record", json);
            }
            catch
            {
                ErrorRecord = new($"StatusCode: {responseMessage.StatusCode}", "A Server Error Occured.");
            }

            loggingService.LogMessage(LocalLoggingService.LogLevel.Debug, $"ApiException: {responseMessage.StatusCode} {responseMessage.RequestMessage?.RequestUri}");
        }
    }
    public record FileStreamWrapper(Stream contentStream, string contentType, string fileName) : IDisposable
    {
        public void Dispose()
        {
            ((IDisposable)contentStream).Dispose();
            GC.SuppressFinalize(this);
        }
    }
    public record ErrorRecord(string type, string error);

    /// <summary>
    /// Login Model used for Logging into the App!
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    public record Login(string email, string password);

public sealed record AuthResponse(
    string userId = "",
    string jwt = "",
    DateTime expires = default,
    string refreshToken = "")
{
    public JsonWebToken? GetJwt()
    {
        if (string.IsNullOrEmpty(jwt))
            return null;

        try
        {
            var token = (JsonWebToken)new JsonWebTokenHandler().ReadToken(jwt);
            return token;
        }
        catch
        {
            return null;
        }
    }
}
public sealed record UserInfo(string firstName, string lastName, string email, int blackbaudId)
{
    private List<string> _roles = [];
    public async Task<List<string>> GetRoles(ApiService api)
    {
        if (_roles.Count == 0)
        {
            _roles = await api.SendAsync<List<string>>(HttpMethod.Get, "api/users/self/roles") ?? [];
        }
        return _roles;
    }

}

public record FileContentResult(string documentId, string fileName, string mimeType, byte[] b64data);

public record PagedResult<T>(
    int page, 
    int pageCount, 
    int pageSize, 
    int totalResults, 
    List<T> items)
{
    public static readonly PagedResult<T> Empty = new(0, 0, 0, 0, []);
}

public static partial class PagedExtensions
{
    public static Uri? IncrementPagedUri(this Uri? uri)
    {
        if (uri is null) return null;

        // `page=\d+`
        var regex = RegexHelper.QueryStringPageParam();
        if (!regex.IsMatch(uri.Query))
            return uri;

        var match = regex.Match(uri.Query);
        var page = int.Parse(match.Value.Split('=')[1]) + 1; // necessarily parsable because of regex.
        
        var newUri = uri.PathAndQuery.Replace(match.Value, $"page={page}");
        return new Uri(newUri);
    }


}