using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Immutable;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.CDRE.ViewModels
{
    public partial class EventsSearchViewModel : ObservableObject, ICachedSearchViewModel<RecurringEventViewModel>
    {
        [ObservableProperty]
        private ImmutableArray<RecurringEventViewModel> available = [];
        [ObservableProperty]
        private ImmutableArray<RecurringEventViewModel> allSelected = [];
        [ObservableProperty]
        private ImmutableArray<RecurringEventViewModel> options = [];
        [ObservableProperty]
        private RecurringEventViewModel selected = RecurringEventViewModel.Default;
        [ObservableProperty]
        private SelectionMode selectionMode = SelectionMode.Single;
        [ObservableProperty]
        private string searchText = "";
        [ObservableProperty]
        private bool isSelected;
        [ObservableProperty]
        private bool showOptions;


        public event EventHandler<ImmutableArray<RecurringEventViewModel>>? OnMultipleResult;
        public event EventHandler<RecurringEventViewModel>? OnSingleResult;
        public event EventHandler? OnZeroResults;

        private readonly CycleDayRecurringEventService cycleDayRecurringEventService;

        public EventsSearchViewModel()
        {
            cycleDayRecurringEventService = ServiceHelper.GetService<CycleDayRecurringEventService>();

            Available = RecurringEventViewModel.ViewModelCache.ToImmutableArray();

            foreach (var Event in Available)
            {
               // day.Selected += Day_Selected;
            }
            
        }

        private void Day_Selected(object? sender, RecurringEventViewModel e)
        {
            throw new NotImplementedException();
        }

        public void Search()
        {
             
        }

        public void Select(RecurringEventViewModel item)
        {
            Day_Selected(this, item);
        }

        async Task IAsyncSearchViewModel<RecurringEventViewModel>.Search()
        {
            await Task.Run(Search);
        }
    }
}
