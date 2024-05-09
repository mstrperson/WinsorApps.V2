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
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Models;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Devices
{
    public partial class DeviceSearchViewModel : ObservableObject, ICachedSearchViewModel<DeviceViewModel>
    {
        [Flags]
        public enum SearchFilter
        {
            WinsorDevices = 0b0110,
            Loaners = 0b1110,
            ActiveOnly = 0b0001,
        }

        private readonly DeviceService _deviceService;

        [ObservableProperty] private ImmutableArray<DeviceViewModel> available;
        [ObservableProperty] private ImmutableArray<DeviceViewModel> allSelected = [];
        [ObservableProperty] private ImmutableArray<DeviceViewModel> options = [];
        [ObservableProperty] private SelectionMode selectionMode = SelectionMode.Single;
        [ObservableProperty] private DeviceViewModel selected;
        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private bool showOptions;

        public event EventHandler<ImmutableArray<DeviceViewModel>>? OnMultipleResult;
        public event EventHandler<DeviceViewModel>? OnSingleResult;
        public event EventHandler<ErrorRecord>? OnError;
        public event EventHandler? OnZeroResults;

        private SearchFilter _filter = SearchFilter.ActiveOnly;
        public SearchFilter Filter 
        { 
            get => _filter;
            set
            {
                _filter = value; 
                ((IAsyncInitService)_deviceService).WaitForInit(err => OnError?.Invoke(this, err))
                    .WhenCompleted(() =>
                    {
                        Available = _deviceService.DeviceCache
                            .Where(GenerateFilter(Filter))
                            .Select(dev => new DeviceViewModel(dev))
                            .ToImmutableArray();
                    });
            }
        }


        public DeviceSearchViewModel()
        {
            _deviceService = ServiceHelper.GetService<DeviceService>();
            ((IAsyncInitService)_deviceService).WaitForInit(err => OnError?.Invoke(this, err))
                .WhenCompleted(() =>
                {
                    Available = _deviceService.DeviceCache
                        .Where(GenerateFilter(Filter))
                        .Select(dev => new DeviceViewModel(dev))
                        .ToImmutableArray();

                    foreach(var dev in Available) 
                    {
                        dev.Selected += Dev_Selected;
                    }
                });

            selected = IEmptyViewModel<DeviceViewModel>.Empty;
        }

        [RelayCommand]
        public void ClearSelection()
        {
            AllSelected = [];
            Options = [];
            Selected = IEmptyViewModel<DeviceViewModel>.Empty;
            IsSelected = false;
            ShowOptions = false;
            SearchText = "";
        }

        [RelayCommand]
        public void Search()
        {
            var possible = Available
                .Where(dev =>
                    dev.SerialNumber.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)
                    || (dev.IsWinsorDevice && dev.WinsorDevice.AssetTag.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)));
            if (!possible.Any())
                OnZeroResults?.Invoke(this, EventArgs.Empty);
            switch (SelectionMode)
            {
                case SelectionMode.Multiple:
                    AllSelected = [.. possible];
                    IsSelected = AllSelected.Length > 0;
                    OnMultipleResult?.Invoke(this, AllSelected);
                    return;
                case SelectionMode.Single:
                    Options = [.. possible];
                    if (Options.Length == 0)
                    {
                        ShowOptions = false;
                        Selected = IEmptyViewModel<DeviceViewModel>.Empty;
                        IsSelected = false;
                        return;
                    }

                    if (Options.Length == 1)
                    {
                        ShowOptions = false;
                        Selected = Options.First();
                        IsSelected = true;
                        SearchText = Selected.IsWinsorDevice ? Selected.WinsorDevice.AssetTag : Selected.SerialNumber;
                        OnSingleResult?.Invoke(this, Selected);
                        return;
                    }

                    ShowOptions = true;
                    Selected = IEmptyViewModel<DeviceViewModel>.Empty;
                    IsSelected = false;
                    return;
                default: return;
            }
        }

        private void Dev_Selected(object? sender, DeviceViewModel e)
        {
            switch (SelectionMode)
            {
                case SelectionMode.Single:
                    Selected = Available.FirstOrDefault(user => user.Id == e.Id) ?? IEmptyViewModel<DeviceViewModel>.Empty;
                    IsSelected = string.IsNullOrEmpty(Selected.Id);
                    Options = [];
                    ShowOptions = false;
                    SearchText = Selected.IsWinsorDevice ? Selected.WinsorDevice.AssetTag : Selected.SerialNumber;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                case SelectionMode.Multiple:
                    var user = Available.FirstOrDefault(user => user.Id == e.Id);
                    if (user is null) return;
                    if (AllSelected.Contains(user))
                        AllSelected = [.. AllSelected.Except([user])];
                    else
                        AllSelected = [.. AllSelected, user];

                    IsSelected = AllSelected.Length > 0;
                    if (IsSelected)
                        OnMultipleResult?.Invoke(this, AllSelected);
                    return;
                case SelectionMode.None:
                default: return;
            }
        }

        public void Select(DeviceViewModel item)
        {
            Dev_Selected(this, item);
        }

        public Func<DeviceRecord, bool> GenerateFilter(SearchFilter filter) => dev =>
        {
            if ((filter & SearchFilter.ActiveOnly) == SearchFilter.ActiveOnly && !dev.isActive)
                return false;

            if ((filter & SearchFilter.WinsorDevices) == SearchFilter.WinsorDevices && !dev.isWinsorDevice)
                return false;

            if ((filter & SearchFilter.Loaners) == SearchFilter.Loaners && (!dev.winsorDevice?.loaner ?? false))
                return false;

            return true;
        };

        async Task IAsyncSearchViewModel<DeviceViewModel>.Search()
        {
            await Task.Run(Search);
        }
    }
}
