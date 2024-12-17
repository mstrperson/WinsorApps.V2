using System.Text;
using System.Text.Json;
using WinsorApps.Services.Global.Services;


namespace WinsorApps.Services.Global.Models;

public interface ISavedCredential
{
    public bool SavedCredExists { get; }

    public Task DeleteSavedCredential();
    public Task SaveJwt(string jwt, string refreshToken);
    public Task Save(string email, string password, string jwt = "", string refreshToken = "");
    public Task<Credential?> GetSavedCredential();

}

public readonly record struct Credential(
        string SavedEmail = "",
        string SavedPassword = "",
        string JWT = "",
        string RefreshToken = "")
{
    public static implicit operator Credential(SavedCredential sc) => new(sc.SavedEmail, sc.SavedPassword, sc.JWT, sc.RefreshToken);
    public static implicit operator SavedCredential(Credential sc) => new(sc.SavedEmail, sc.SavedPassword, sc.JWT, sc.RefreshToken);
}

[Obsolete("Using the CredentialManager object now!")]
public record SavedCredential(
        string SavedEmail = "",
        string SavedPassword = "",
        string JWT = "",
        string RefreshToken = "") : ISavedCredential
{
    private static SavedCredential _default = new();
    public static ISavedCredential Default => _default;

    private byte[]? _appGuid;

    private string GuidFilePath => $"{LocalLoggingService.AppDataPath}{Path.DirectorySeparatorChar}.forms-app.guid";
    private string GuidFilePathOld => $"{LocalLoggingService.AppDataPathOld}{Path.DirectorySeparatorChar}forms-app.guid";
   
    public bool SavedCredExists => File.Exists(CredFilePath);

    private byte[] ApplicationGuid
    {
        get
        {
            if (_appGuid is null)
            {
                if (!File.Exists(GuidFilePath) && !File.Exists(GuidFilePathOld))
                {
                    byte[] guid = Guid.NewGuid().ToByteArray();
                    File.WriteAllBytes(GuidFilePath, guid);
                    _appGuid = guid;
                }
                
                if(!File.Exists(GuidFilePath))
                {
                    File.Copy(GuidFilePathOld, GuidFilePath, true);
                    File.Delete(GuidFilePathOld);
                }
                
                _appGuid = File.ReadAllBytes(GuidFilePath);
            }

            return _appGuid;
        }
    }

    private string CredFilePath => $"{LocalLoggingService.AppDataPath}{Path.DirectorySeparatorChar}.login.cred";
    private string CredFilePathOld => $"{LocalLoggingService.AppDataPathOld}{Path.DirectorySeparatorChar}login.cred";

    public  async Task DeleteSavedCredential()
    {
        if (File.Exists(CredFilePath))
            File.Delete(CredFilePath);

        await Task.CompletedTask;
    }

    public async Task SaveJwt(string jwt, string refreshToken)
    {
        var saved = await GetSavedCredential() ?? new();

        saved = saved with { JWT = jwt, RefreshToken = refreshToken };

        await WriteFileData(saved);
    }
        

    public  async Task Save(string email, string password, string jwt = "", string refreshToken = "") =>
        await WriteFileData(new SavedCredential(email, password, jwt, refreshToken));

    public  async Task WriteFileData(SavedCredential credential)
    {

        var json = JsonSerializer.Serialize(credential);
        byte[] credBytes = Encoding.UTF8.GetBytes(json);

        await File.WriteAllBytesAsync(CredFilePath, credBytes);
    }

    public  async Task<Credential?> GetSavedCredential()
    {
        if (!File.Exists(CredFilePath) && !File.Exists(CredFilePathOld))
            return null;

        if(!File.Exists(CredFilePath))
        {
            File.Copy(CredFilePathOld, CredFilePath, true);
            File.Delete(CredFilePathOld);
        }

        byte[] outputBytes = await File.ReadAllBytesAsync(CredFilePath);
   
        string json = Encoding.UTF8.GetString(outputBytes);
        json = json.Substring(0, json.IndexOf("}") + 1);

        if (!json.StartsWith('{') || !json.EndsWith('}'))
        {
            await DeleteSavedCredential();
            return null;
        }

        try
        {
            var credential = JsonSerializer.Deserialize<Credential>(json);
            return credential;
        }
        catch { return null; }
    }
}
