using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
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
                ErrorRecord = (ErrorRecord)JsonSerializer.Deserialize(responseMessage.Content.ReadAsStream(), typeof(ErrorRecord))!;
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
        }
    }
    public readonly record struct ErrorRecord(string type, string error);

    /// <summary>
    /// Login Model used for Logging into the App!
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    public readonly record struct Login(string email, string password);

    public sealed record AuthResponse(string userId = "", string jwt = "", DateTime expires = default, string refreshToken = "")
    {
        public JwtSecurityToken? GetJwt()
        {
            if (string.IsNullOrEmpty(jwt))
                return null;

            try
            {
                return (JwtSecurityToken)new JwtSecurityTokenHandler().ReadToken(jwt);
            }
            catch
            {
                return null;
            }
        }
    }
    public sealed record UserInfo(string firstName, string lastName, string email, int blackbaudId)
    {
        private ImmutableArray<string>? _roles = null;
        public async Task<ImmutableArray<string>> GetRoles(ApiService api)
        {
            if (!_roles.HasValue)
            {
                _roles = await api.SendAsync<ImmutableArray<string>>(HttpMethod.Get, "api/users/self/roles");
            }
            return _roles.Value;
        }

    }

public readonly record struct FileContentResult(string documentId, string fileName, string mimeType, byte[] b64data);

    
