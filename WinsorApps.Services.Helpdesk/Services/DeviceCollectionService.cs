using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.Services.Helpdesk.Services;

public class DeviceCollectionService(ApiService api) :
    IAsyncInitService
{
    public string CacheFileName => "";

    public bool Started { get; private set; }

    public bool Ready { get; private set; }

    public double Progress { get; private set; }

    public List<CollectionEntry> CollectionEntries { get; private set; } = [];

    public void ClearCache()
    {
    }

    public async Task<List<CollectionEntry>> GetOpenCollections(ErrorAction onError) =>
        (await api.SendAsync<List<CollectionEntry>>(HttpMethod.Get, "api/helpdesk/collection", onError: onError)) ?? [];

    public async Task Initialize(ErrorAction onError)
    {
        CollectionEntries = await GetOpenCollections(onError);
        Progress = 1;
        Ready = true;
    }

    public bool LoadCache()
    {
        return true;
    }

    public async Task<CollectionEntry?> PostEntry(CreateCollectionEntry entry, ErrorAction onError) =>
        await api.SendAsync<CreateCollectionEntry, CollectionEntry>(HttpMethod.Post, "api/helpdesk/collection", entry, onError: onError);

    public async Task Refresh(ErrorAction onError) => await Initialize(onError);

    public async Task SaveCache() => await Task.CompletedTask;

    public async Task<CollectionEntry?> UpdateEntry(string id, CreateCollectionEntry entry, ErrorAction onError) =>
        await api.SendAsync<CreateCollectionEntry, CollectionEntry>(HttpMethod.Put, $"api/helpdesk/collection/{id}", entry, onError: onError);

    public async Task<(byte[], string)> DownloadReport(ErrorAction onError)
    {
        var result = await api.DownloadFile("api/helpdesk/collection/report", onError: onError);
        return (result ?? [], "Report.csv");
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }
}
