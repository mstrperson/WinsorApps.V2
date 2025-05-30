﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared;

public class CredentialManager : ISavedCredential
{
    private static readonly string credKey = "edu.winsor.forms_credential";

    public async Task CheckSavedCredentials()
    {
        var thisCred = await GetSavedCredential();
        if (thisCred is not null) return;

        SavedCredential sc = new();
        if (!sc.SavedCredExists) return;

        var result = await sc.GetSavedCredential();
        if (result is null) return;

        var cred = result;
        await Save(cred.SavedEmail, cred.SavedPassword, cred.JWT, cred.RefreshToken);
    }

    public bool SavedCredExists { get; private set; } = false;

    public async Task DeleteSavedCredential()
    {
        SecureStorage.Remove(credKey);
        SavedCredExists = false;
        await Task.CompletedTask;
    }

    public async Task<Credential?> GetSavedCredential()
    {
        SavedCredExists = false;
        var json = await SecureStorage.GetAsync(credKey);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            var cred = JsonSerializer.Deserialize<Credential>(json);
            SavedCredExists = true;
            return cred;
        }
        catch
        {
            return null;
        }
    }

    public async Task Save(string email, string password, string jwt = "", string refreshToken = "")
    {
        Credential credential = new(email, password, jwt, refreshToken);
        var json = JsonSerializer.Serialize(credential);
        await SecureStorage.SetAsync(credKey, json);
        SavedCredExists = true;
    }

    public async Task SaveJwt(string jwt, string refreshToken)
    {
        var credential = await GetSavedCredential() ?? new();
        credential = credential with {JWT = jwt, RefreshToken = refreshToken};
        var json = JsonSerializer.Serialize(credential);
        await SecureStorage.SetAsync(credKey, json);
        SavedCredExists = true;
    }
}

