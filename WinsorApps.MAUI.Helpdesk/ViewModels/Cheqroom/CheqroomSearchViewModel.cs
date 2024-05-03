using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom;

public partial class CheqroomCheckoutSearchViewModel : ObservableObject, IAsyncSearchViewModel<CheqroomItemViewModel>
{
    private readonly CheqroomService _cheqroom;

    [ObservableProperty] private ImmutableArray<CheqroomItemViewModel> allSelected = [];
    [ObservableProperty] private ImmutableArray<CheqroomItemViewModel> options = [];
    [ObservableProperty] private SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty] private CheqroomItemViewModel selected;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool showOptions;

    public event EventHandler<ErrorRecord>? OnError;

    public event EventHandler<ImmutableArray<CheqroomItemViewModel>>? OnMultipleResult;
    public event EventHandler<CheqroomItemViewModel>? OnSingleResult;

    public CheqroomCheckoutSearchViewModel()
    {
        _cheqroom = ServiceHelper.GetService<CheqroomService>();
        selected = IEmptyViewModel<CheqroomItemViewModel>.Empty;
    }

    [RelayCommand]
    public async Task Search()
    {
    }

    public void Select(CheqroomItemViewModel item)
    {

    }
}
