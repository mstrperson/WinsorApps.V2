using System.Text;
using System.Text.Json;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Global.Models;
public record SavedCredential(
        string SavedEmail = "",
        string SavedPassword = "",
        string JWT = "",
        string RefreshToken = "")
{
    private static byte[]? _appGuid;

    private static string GuidFilePath => $"{LocalLoggingService.AppDataPath}{Path.DirectorySeparatorChar}.forms-app.guid";
    private static string GuidFilePathOld => $"{LocalLoggingService.AppDataPathOld}{Path.DirectorySeparatorChar}forms-app.guid";



    private static byte[] ApplicationGuid
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

    private static string CredFilePath => $"{LocalLoggingService.AppDataPath}{Path.DirectorySeparatorChar}.login.cred";
    private static string CredFilePathOld => $"{LocalLoggingService.AppDataPathOld}{Path.DirectorySeparatorChar}login.cred";

    public static void DeleteSavedCredential()
    {
        if (File.Exists(CredFilePath))
            File.Delete(CredFilePath);
    }

    public static async void SaveJwt(string jwt, string refreshToken)
    {
        var saved = await GetSavedCredential();
        if (saved is null)
            saved = new();

        saved = saved with { JWT = jwt, RefreshToken = refreshToken };

        await WriteFileData(saved);
    }
        

    public static async void Save(string email, string password, string jwt = "", string refreshToken = "") =>
        await WriteFileData(new SavedCredential(email, password, jwt, refreshToken));

    public static async Task WriteFileData(SavedCredential credential)
    {
        var json = JsonSerializer.Serialize(credential);
        byte[] credBytes = Encoding.UTF8.GetBytes(json);

        await File.WriteAllBytesAsync(CredFilePath, credBytes);
    }

    public static bool HasSavedCred => File.Exists(CredFilePath);

    public static async Task<SavedCredential?> GetSavedCredential()
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
            DeleteSavedCredential();
            return null;
        }

        var credential = JsonSerializer.Deserialize<SavedCredential>(json);

        return credential;
    }
}
