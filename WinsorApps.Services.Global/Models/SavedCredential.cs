using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.Global.Models;
public record SavedCredential(
        string SavedEmail = "",
        string SavedPassword = "",
        string JWT = "",
        string RefreshToken = "")
{
    private static byte[]? _appGuid;

    private static byte[] ApplicationGuid
    {
        get
        {
            if (_appGuid is null)
            {
                char separator = Environment.OSVersion.Platform == PlatformID.Win32NT ? '\\' : '/';
                var guidFilePath =
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{separator}forms-app.guid";

                if (!File.Exists(guidFilePath))
                {
                    byte[] guid = Guid.NewGuid().ToByteArray();
                    File.WriteAllBytes(guidFilePath, guid);
                    _appGuid = guid;
                }
                else
                    _appGuid = File.ReadAllBytes(guidFilePath);
            }

            return _appGuid;
        }
    }

    private static string CredFilePath
    {
        get
        {
            char separator = Environment.OSVersion.Platform == PlatformID.Win32NT ? '\\' : '/';
            return $"{LocalLoggingService.AppDataPath}{separator}login.cred";
        }
    }

    public static void DeleteSavedCredential()
    {
        if (File.Exists(CredFilePath))
            File.Delete(CredFilePath);
    }

    public static async void SaveJwt(string jwt, string refreshToken) =>
        await WriteFileData(new SavedCredential(JWT: jwt, RefreshToken: refreshToken));

    public static async void Save(string email, string password) =>
        await WriteFileData(new SavedCredential(email, password));

    public static async Task WriteFileData(SavedCredential credential)
    {
        var json = JsonSerializer.Serialize(credential);
        byte[] credBytes = Encoding.UTF8.GetBytes(json);

        int mult = 1;
        for (; mult * ApplicationGuid.Length < credBytes.Length; mult++) ;

        byte[] outputBytes = new byte[credBytes.Length + ApplicationGuid.Length * mult];

        for (int i = 0, j = 0, k = 0; i < outputBytes.Length; i++)
        {
            if (i % 2 == 0 && j < credBytes.Length)
            {
                outputBytes[i] = credBytes[j++];
            }
            else
            {
                outputBytes[i] = ApplicationGuid[(k++) % ApplicationGuid.Length];
            }
        }

        for (int i = 0; i < outputBytes.Length; i++)
        {
            outputBytes[i] = (byte)(outputBytes[i] ^ ApplicationGuid[i % ApplicationGuid.Length]);
        }

        await File.WriteAllBytesAsync(CredFilePath, outputBytes);
    }

    public static async Task<SavedCredential?> GetSavedCredential()
    {
        if (!File.Exists(CredFilePath))
            return null;

        byte[] outputBytes = await File.ReadAllBytesAsync(CredFilePath);

        for (int i = 0; i < outputBytes.Length; i++)
        {
            outputBytes[i] = (byte)(outputBytes[i] ^ ApplicationGuid[i % ApplicationGuid.Length]);
        }

        byte[] credBytes = new byte[outputBytes.Length / 2];
        for (int i = 0; i < credBytes.Length; i++)
        {
            credBytes[i] = outputBytes[2 * i];
        }

        string json = Encoding.UTF8.GetString(credBytes);
        json = json.Substring(0, json.IndexOf("}") + 1);
        var credential = JsonSerializer.Deserialize<SavedCredential>(json);

        return credential;
    }
}
