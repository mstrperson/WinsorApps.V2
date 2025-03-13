using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class FreeBlockViewModel : ObservableObject,
    IModelCarrier<FreeBlockViewModel, BlockMeetingTime>,
    ISelectable<FreeBlockViewModel>
{
    [ObservableProperty] string blockName = "";
    [ObservableProperty] DateTime start;
    [ObservableProperty] DateTime end;

    public Optional<BlockMeetingTime> Model { get; set; } = Optional<BlockMeetingTime>.None();
    [ObservableProperty] bool isSelected;

    public event EventHandler<FreeBlockViewModel>? Selected;

    public static FreeBlockViewModel Get(BlockMeetingTime model) => new()
    {
        BlockName = model.block.name,
        Start = model.start,
        End=model.end,
        Model = Optional<BlockMeetingTime>.Some(model)
    };

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class FreeBlockCollectionViewModel : ObservableObject,
    IErrorHandling
{
    private readonly RegistrarService _registrar = ServiceHelper.GetService<RegistrarService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<FreeBlockViewModel>? FreeBlockSelected;

    [ObservableProperty] DateTime start;
    [ObservableProperty] DateTime end;
    [ObservableProperty] ObservableCollection<FreeBlockViewModel> freeBlocks = [];
    [ObservableProperty] UserViewModel user = UserViewModel.Empty;

    [RelayCommand]
    public async Task LoadFreeBlocks()
    {
        if (End < Start)
            (Start, End) = (End, Start);
        DateRange inRange = new(Start, End);
        var result = await _registrar.GetFreeBlocksFor(User.Id, inRange, OnError.DefaultBehavior(this));
        FreeBlocks = [.. result.freeBlocks.Select(FreeBlockViewModel.Get)];
        foreach (var free in FreeBlocks)
            free.Selected += (_, _) => 
                FreeBlockSelected?.Invoke(this, free);
    }
}
