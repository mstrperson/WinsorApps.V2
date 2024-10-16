﻿using System.Text;
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
        if (!File.Exists(CredFilePath) && !File.Exists(CredFilePathOld))
            return null;

        if(!File.Exists(CredFilePath))
        {
            File.Copy(CredFilePathOld, CredFilePath, true);
            File.Delete(CredFilePathOld);
        }

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

        if (!json.StartsWith('{') || !json.EndsWith('}'))
        {
            DeleteSavedCredential();
            return null;
        }

        var credential = JsonSerializer.Deserialize<SavedCredential>(json);

        return credential;
    }
}
