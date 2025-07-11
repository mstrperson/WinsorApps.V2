﻿using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Storage;
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

    [ObservableProperty] private string id = "";
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";
    [ObservableProperty] private string assetTag = "";
    [ObservableProperty] private string chargerAssetTag = "";
    [ObservableProperty] private bool hasCord = true;
    [ObservableProperty] private bool parentalLockEnabled = false;
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private UserViewModel user = UserViewModel.Empty;
    [ObservableProperty] private DateTime timestamp = DateTime.Now;
    [ObservableProperty] private bool hasUser;
    [ObservableProperty] private bool showCancel;

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
            ParentalLockEnabled = model.parentalLock,
            Notes = model.notes,
            User = string.IsNullOrEmpty(model.student.id) ? UserViewModel.Empty : UserViewModel.Get(model.student),
            Timestamp = model.timestamp,
            HasUser = !string.IsNullOrEmpty(model.student.id)
        };

        if (vm.HasUser)
        {
            vm.User.GetPhotoCommand.Execute(null);
        }
        
        return vm;
    }

    [RelayCommand]
    public void ToggleParentalLock() => ParentalLockEnabled = !ParentalLockEnabled;

    [RelayCommand]
    public void ToggleHasCord() => HasCord = !HasCord;

    [RelayCommand]
    public void Clear()
    {
        AssetTag = "";
        ChargerAssetTag = "";
        HasCord = true;
        ParentalLockEnabled = false;
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
            ParentalLockEnabled,
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
            ParentalLockEnabled = result.parentalLock;
            Notes = result.notes;
            User = string.IsNullOrEmpty(result.student.id) ? UserViewModel.Empty : UserViewModel.Get(result.student);
            Timestamp = result.timestamp;
            HasUser = !string.IsNullOrEmpty(result.student.id);
            if (HasUser)
            {
                User.GetPhotoCommand.Execute(null);
            }
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
    [ObservableProperty] private ObservableCollection<DeviceCollectionViewModel> collectionEntries = [];
    [ObservableProperty] private DeviceCollectionViewModel openEntry;
    [ObservableProperty] private DeviceCollectionViewModel prevEntry;
    [ObservableProperty] private bool showPrev;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string busyMessage = "";

    private static readonly double _headerHeight = 40;
    private static readonly double _rowHeight = 25;
    [ObservableProperty] private double collectionHeight = 500;

    private async Task BackgroundTask()
    {
        await Task.Delay(TimeSpan.FromMinutes(2));
        await LoadEntries();
    }
    public DeviceCollectionPageViewModel()
    {
        LoadEntries().SafeFireAndForget(e => e.LogException());
        OpenEntry = CreateEmptyCVM();
        CollectionEntries.CollectionChanged += CollectionEntries_CollectionChanged;
    }

    private void CollectionEntries_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        CollectionHeight = Math.Min(500, _rowHeight * (CollectionEntries.Count) + _headerHeight);
    }

    private DeviceCollectionViewModel CreateEmptyCVM()
    {
        var entry = new DeviceCollectionViewModel(); 
        entry.OnError += (sender, error) => OnError?.Invoke(sender, error);
        entry.Selected += (_, vm) =>
        {
            OpenEntry = vm;
            ShowPrev = false;
            vm.ShowCancel = true;
        };
        entry.Submitted += (_, _) =>
        {
            PrevEntry = entry;
            entry.ShowCancel = true;
            ShowPrev = true;
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

        Busy = true;
        BusyMessage = "Downloading Report...";
        var (data, fileName) = await service.DownloadReport(OnError.DefaultBehavior(this));
        using MemoryStream ms = new(data);
        var result = await FileSaver.SaveAsync(fileName, ms);
        if (!result.IsSuccessful)
        {
            OnError?.Invoke(this, new ErrorRecord("Download Failed", result.Exception?.Message ?? "Download Not Saved"));
        }
        Busy = false;
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
                ShowPrev = false;
            };
            entry.Submitted += (_, _) =>
            {
                PrevEntry = entry;
                ShowPrev = true;
                OpenEntry = CreateEmptyCVM();
                OpenEntry.Clear();
                LoadEntries().SafeFireAndForget(e => e.LogException());
            };
            entry.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
        }
        Busy = false;
    }
}


