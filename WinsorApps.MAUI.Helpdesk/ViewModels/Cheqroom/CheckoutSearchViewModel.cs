
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Csv;
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
    public partial class CheckoutSearchViewModel : ObservableObject, ICachedSearchViewModel<CheckoutSearchResultViewModel>, IErrorHandling, IMultiModalSearch<CheckoutSearchResultViewModel>
    {
        public CheckoutSearchViewModel Self => this;

        private readonly CheqroomService _cheqroom;

        [ObservableProperty] private ImmutableArray<CheckoutSearchResultViewModel> available;
        [ObservableProperty] private ImmutableArray<CheckoutSearchResultViewModel> allSelected = [];
        [ObservableProperty] private SelectionMode selectionMode = SelectionMode.Single;
        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private bool showOptions;
        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private ImmutableArray<CheckoutSearchResultViewModel> options = [];
        [ObservableProperty] private CheckoutSearchResultViewModel selected;
        [ObservableProperty] private bool working;

        [ObservableProperty]
        public ImmutableArray<string> searchModes = ["By Asset Tag", "By User"];

        public Func<CheckoutSearchResultViewModel, bool> SearchFilter =>
            SearchMode switch
            {
                "By Asset Tag" => ord => ord.Items.Any(item => item.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)),
                "By User" => ord => ord.User.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase),
                _ => _ => true
            };

        [ObservableProperty]
        private string searchMode;

        public event EventHandler<ErrorRecord>? OnError;
        public event EventHandler<ImmutableArray<CheckoutSearchResultViewModel>>? OnMultipleResult;
        public event EventHandler<CheckoutSearchResultViewModel>? OnSingleResult;

        public event EventHandler? OnZeroResults;

        public event EventHandler<CheckoutSearchResultViewModel>? OnCheckedIn;

        public event EventHandler<(byte[], string)>? OnExport;

        public CheckoutSearchViewModel(CheqroomService cheqroom)
        {
            _cheqroom = cheqroom;
            _cheqroom.OnCacheRefreshed += 
                (_, _) => 
                LoadCheckouts();
            searchMode = SearchModes[0];
            selected = CheckoutSearchResultViewModel.Default;
            LoadCheckouts();
        }

        [RelayCommand]
        public void Clear()
        {
            SearchText = "";
            Options = [];
            Selected = CheckoutSearchResultViewModel.Default;
            AllSelected = [];
        }

        [RelayCommand]
        public void LoadCheckouts()
        {
            Available = CheckoutSearchResultViewModel.GetClonedViewModels(_cheqroom.OpenOrders).ToImmutableArray();
            foreach (var order in Available)
            {
                order.OnError += (sender, e) => OnError?.Invoke(sender, e);
                order.OnCheckedIn += (sender, e) => 
                    OnCheckedIn?.Invoke(sender, e);
            }
        }

        [RelayCommand]
        public async Task Refresh()
        {
            Working = true;
            await _cheqroom.Refresh(OnError.DefaultBehavior(this));
            Clear();
            Working = false;
        }

        [RelayCommand]
        public void Search()
        {
            if (Available.Length == 0)
                LoadCheckouts();

            var possible = Available
                .Where(SearchFilter);
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
                        Selected = CheckoutSearchResultViewModel.Default;
                        IsSelected = false;
                        OnZeroResults?.Invoke(this, EventArgs.Empty);
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
                    Selected = CheckoutSearchResultViewModel.Default;
                    IsSelected = false;
                    return;
                default: return;
            }
        }

        public void Select(CheckoutSearchResultViewModel item)
        {
            Selected = item;
            IsSelected = true;
            OnSingleResult?.Invoke(this, item);
        }

        async Task IAsyncSearchViewModel<CheckoutSearchResultViewModel>.Search() => await Task.Run(Search);

        [RelayCommand]
        public async Task Export()
        {
            CSV output = 
                new(
                Options.Select(ord => new Row
                {
                    { "User", ord.User.DisplayName },
                    { "Item(s)", ord.Items.DelimeteredList() },
                    { "Due", $"{ord.Due:dd MMMM yyyy}" },
                    { "Overdue", ord.IsOverdue ? "X" : "" }
                })
                .ToList());

            using MemoryStream ms = new();
            output.Save(ms);

            var data = ms.ToArray();
            OnExport?.Invoke(this, (data, $"open-checkouts-{SearchMode}-{SearchText}.csv".ToLowerInvariant().Replace(' ', '-')));
        }
    }
}
