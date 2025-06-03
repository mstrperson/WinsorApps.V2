using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels;

public partial class DeviceCollectionViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel,
    IModelCarrier<DeviceCollectionViewModel, CollectionEntry>,
    ISelectable<DeviceCollectionViewModel>
{
    private readonly DeviceCollectionService _service = ServiceHelper.GetService<DeviceCollectionService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<DeviceCollectionViewModel>? Selected;
    public event EventHandler? Submitted;

    [ObservableProperty] string id = "";
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] string assetTag = "";
    [ObservableProperty] string chargerAssetTag = "";
    [ObservableProperty] bool hasCord = true;
    [ObservableProperty] string notes = "";
    [ObservableProperty] bool isSelected;
    [ObservableProperty] UserViewModel user = UserViewModel.Empty;
    [ObservableProperty] DateTime timestamp = DateTime.Now;
    [ObservableProperty] bool hasUser;

    public Optional<CollectionEntry> Model { get; private set; } = Optional<CollectionEntry>.None();

    public static DeviceCollectionViewModel Get(CollectionEntry model)
    {
        var vm = new DeviceCollectionViewModel
        {
            Id = model.id,
            Model = Optional<CollectionEntry>.Some(model),
            AssetTag = model.assetTag,
            ChargerAssetTag = model.chargerAssetTag,
            HasCord = model.hasCord,
            Notes = model.notes,
            User = string.IsNullOrEmpty(model.student.id) ? UserViewModel.Empty : UserViewModel.Get(model.student),
            Timestamp = model.timestamp,
            HasUser = !string.IsNullOrEmpty(model.student.id)
        }; 
        
        return vm;
    }

    [RelayCommand]
    public void ToggleHasCord() => HasCord = !HasCord;

    [RelayCommand]
    public void Clear()
    {
        AssetTag = "";
        ChargerAssetTag = "";
        HasCord = true;
        Id = "";
        Model = Optional<CollectionEntry>.None();
        Timestamp = default;
        Notes = "";
        User = UserViewModel.Empty;
        HasUser = false;
    }

    [RelayCommand]
    public async Task Submit()
    {
        var createEntry = new CreateCollectionEntry(
            AssetTag,
            ChargerAssetTag,
            HasCord,
            Notes
        );

        var result = Id switch
        {
            "" => await _service.PostEntry(createEntry, OnError.DefaultBehavior(this)),
            _ => await _service.UpdateEntry(Id, createEntry, OnError.DefaultBehavior(this))
        };

        if(result is not null)
        {
            Model = Optional<CollectionEntry>.Some(result);
            Id = result.id;
            AssetTag = result.assetTag;
            ChargerAssetTag = result.chargerAssetTag;
            HasCord = result.hasCord;
            Notes = result.notes;
            User = string.IsNullOrEmpty(result.student.id) ? UserViewModel.Empty : UserViewModel.Get(result.student);
            Timestamp = result.timestamp;
            HasUser = !string.IsNullOrEmpty(result.student.id);
            Submitted?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class DeviceCollectionPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly DeviceCollectionService service = ServiceHelper.GetService<DeviceCollectionService>();
    [ObservableProperty] ObservableCollection<DeviceCollectionViewModel> collectionEntries = [];
    [ObservableProperty] DeviceCollectionViewModel openEntry;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    private async Task BackgroundTask()
    {
        await Task.Delay(TimeSpan.FromMinutes(2));
        await LoadEntries();
    }
    public DeviceCollectionPageViewModel()
    {
        LoadEntries().SafeFireAndForget(e => e.LogException());
        OpenEntry = CreateEmptyCVM();
        BackgroundTask().SafeFireAndForget(e => e.LogException());
    }

    private DeviceCollectionViewModel CreateEmptyCVM()
    {
        var entry = new DeviceCollectionViewModel(); 
        entry.OnError += (sender, error) => OnError?.Invoke(sender, error);
        entry.Selected += (_, vm) =>
        {
            OpenEntry = vm;
        };
        entry.Submitted += (_, _) =>
        {
            OpenEntry = CreateEmptyCVM();
            OpenEntry.Clear();
            LoadEntries().SafeFireAndForget(e => e.LogException());
        };
        entry.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        return entry;
    }

    public event EventHandler<ErrorRecord>? OnError;

    [RelayCommand]
    private async Task DownloadReport()
    {
        await LoadEntries();
        if (CollectionEntries.Count == 0)
        {
            OnError?.Invoke(this, new ErrorRecord("No Entries", "No entries to download"));
            return;
        }
    }

    [RelayCommand]
    private async Task LoadEntries()
    {
        Busy = true;
        BusyMessage = "Syncing Collection Info";
        var entries = await service.GetOpenCollections(OnError.DefaultBehavior(this));
        CollectionEntries = [.. entries.Select(DeviceCollectionViewModel.Get)];
        foreach(var entry in CollectionEntries)
        {
            entry.OnError += (sender, error) => OnError?.Invoke(sender, error);
            entry.Selected += (_, vm) =>
            {
                OpenEntry = vm;
            };
            entry.Submitted += (_, _) =>
            {
                OpenEntry = CreateEmptyCVM();
                LoadEntries().SafeFireAndForget(e => e.LogException());
            };
            entry.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }
        Busy = false;
    }
}


