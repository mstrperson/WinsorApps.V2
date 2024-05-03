
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
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom
{
    public partial class QuickCheckinViewModel : ObservableObject, ICachedSearchViewModel<CheckoutSearchResultViewModel>, IErrorHandling
    {
        private readonly CheqroomService _cheqroom;

        [ObservableProperty] private ImmutableArray<CheckoutSearchResultViewModel> available;

        [ObservableProperty]
        private ImmutableArray<CheckoutSearchResultViewModel> allSelected = [];

        [ObservableProperty] private SelectionMode selectionMode;
        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private bool showOptions;
        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private ImmutableArray<CheckoutSearchResultViewModel> options;

        [ObservableProperty] private CheckoutSearchResultViewModel selected;

        public event EventHandler<ErrorRecord>? OnError;
        public event EventHandler<ImmutableArray<CheckoutSearchResultViewModel>>? OnMultipleResult;
        public event EventHandler<CheckoutSearchResultViewModel>? OnSingleResult;

        public QuickCheckinViewModel(CheqroomService cheqroom)
        {
            _cheqroom = cheqroom;
            var cacheTask = _cheqroom.WaitForInit(OnError.DefaultBehavior(this));
            cacheTask.WhenCompleted(() =>
            {
                Available = _cheqroom.OpenOrders.Select(o => new CheckoutSearchResultViewModel(o)).ToImmutableArray();
            });
        }

        [RelayCommand]
        public void Search()
        {
            var possible = Available
                .Where(ord => ord.Items.Any(item => item.AssetTag.Equals(SearchText, StringComparison.InvariantCultureIgnoreCase))); switch (SelectionMode)
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
                        Selected = IEmptyViewModel<CheckoutSearchResultViewModel>.Empty;
                        IsSelected = false;
                        return;
                    }

                    if (Options.Length == 1)
                    {
                        ShowOptions = false;
                        Selected = Options.First();
                        IsSelected = true;
                        OnSingleResult?.Invoke(this, Selected);
                        return;
                    }

                    ShowOptions = true;
                    Selected = IEmptyViewModel<CheckoutSearchResultViewModel>.Empty;
                    IsSelected = false;
                    return;
                default: return;
            }
        }

        public void Select(CheckoutSearchResultViewModel item)
        {

        }

        async Task IAsyncSearchViewModel<CheckoutSearchResultViewModel>.Search() => await Task.Run(Search);
    }
}
