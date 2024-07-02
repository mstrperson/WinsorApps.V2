using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.CDRE.ViewModels
{
    public partial class CycleDaySelectionViewModel : ObservableObject, ICheckBoxListViewModel<SelectableLabelViewModel>
    {
        [ObservableProperty]
        private ImmutableArray<SelectableLabelViewModel> items = [];

        public SelectableLabelViewModel? this[string cycleday]
        {
            get
            {
                foreach (var item in Items) { 
                    if(item.Label == cycleday) return item;
                }
                return null;
            }
        }
        public CycleDaySelectionViewModel()
        {
            items = [
                new() {Label = "Day 1"},
                new() {Label = "Day 2"},
                new() {Label = "Day 3"},
                new() {Label = "Day 4"},
                new() {Label = "Day 5"},
                new() {Label = "Day 6"}
            ];
        }
    }

    public partial class EventListViewModel: ObservableObject, IErrorHandling
    {
        private readonly CycleDayRecurringEventService _eventService = ServiceHelper.GetService<CycleDayRecurringEventService>();
        private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

        [ObservableProperty] ImmutableArray<RecurringEventViewModel> events = [];
        public event EventHandler<RecurringEventViewModel>? CreateRequested;
        public event EventHandler<RecurringEventViewModel>? Reload;
        public event EventHandler<RecurringEventViewModel>? EditRequested;
        public event EventHandler<ErrorRecord>? OnError;

        [ObservableProperty] bool isBusy;

        [RelayCommand]
        public void LoadEvents()
        {
            IsBusy = true;
            Events = RecurringEventViewModel.GetClonedViewModels(_eventService.RecurringEvents).ToImmutableArray();
            foreach (var evt in Events)
            {
                evt.OnError += (sender, e) => OnError?.Invoke(sender, e);
                evt.Selected += (sender, e) => EditRequested?.Invoke(sender, e);
                evt.OnCreated += (sender, e) =>
                {
                    Events = Events.Add(e);
                    Reload?.Invoke(sender, e);
                };
                evt.OnUpdated += (sender, e) =>
                {
                    Events = Events.Replace(evt, e);
                    Reload?.Invoke(sender, e);
                };
                evt.OnDelete += (sender, e) =>
                {
                    Events = Events.Remove(evt);
                    Reload?.Invoke(sender, e);
                };
            }
            IsBusy = false;
        }

        [RelayCommand]
        public void Create()
        {
            _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"{nameof(CreateCommand)} create started");
            var evt = new RecurringEventViewModel();
            evt.OnError += (sender, e) => OnError?.Invoke(sender, e);
            evt.Selected += (sender, e) => EditRequested?.Invoke(sender, e);
            evt.OnCreated += (sender, e) => 
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{e.Summary} {e.Id} created");
                Events = Events.Add(e);
                Reload?.Invoke(sender, e);
            };
            evt.OnUpdated += (sender, e) =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{e.Summary} {e.Id} updated");
                Events = Events.Replace(evt, e);
                Reload?.Invoke(sender, e);
            };
            evt.OnDelete += (sender, e) =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{e.Summary} {e.Id} deleted");
                Events = Events.Remove(evt);
                Reload?.Invoke(sender, e);
            };
            CreateRequested?.Invoke(this, evt);
        }
    }
    public partial class RecurringEventViewModel :
        ObservableObject,
        ICachedViewModel<RecurringEventViewModel, CycleDayRecurringEvent, CycleDayRecurringEventService>,
        ISelectable<RecurringEventViewModel>,
        IDefaultValueViewModel<RecurringEventViewModel>,
        IErrorHandling,
        IBusyViewModel
    {
        private readonly CycleDayRecurringEventService _eventService = ServiceHelper.GetService<CycleDayRecurringEventService>();

        [ObservableProperty] string id = "";
        [ObservableProperty] DateTime beginning = DateTime.Today;
        [ObservableProperty] DateTime ending = DateTime.Today;
        [ObservableProperty] string creatorId = "";
        [ObservableProperty] string summary = "";
        [ObservableProperty] string description = "";
        [ObservableProperty] EmailListViewModel attendees = new();
        [ObservableProperty] bool allDay;
        [ObservableProperty] TimeSpan startTime;
        [ObservableProperty] TimeSpan endTime;
        [ObservableProperty] CycleDaySelectionViewModel cycleDays = new();
        [ObservableProperty] int frequency = 1;
        [ObservableProperty] bool isPublic;
        [ObservableProperty] bool showDelete = false;
        [ObservableProperty] int[] frequencyOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        [ObservableProperty] bool busy;
        [ObservableProperty] string busyMessage = "Loading";
       
        public int Duration => (int)(EndTime - StartTime).TotalMinutes;

        // TODO:  Add More Observable Properties for all the relevant
        //        things for a CycleDayRecurringEvent

        public RecurringEventViewModel()
        {
            
        }

        public event EventHandler<RecurringEventViewModel>? OnCreated;
        public event EventHandler<RecurringEventViewModel>? OnUpdated;
        public event EventHandler<RecurringEventViewModel>? OnDelete;
        public event EventHandler<RecurringEventViewModel>? EditRequested;

        [RelayCommand]
        public async Task Submit()
        {
            Busy = true;
            if(!CycleDays.Items.Any(item => item.IsSelected))
            {
                OnError?.Invoke(this, new("", "Required Fields for Cycle Days"));
                Busy = false;
                return;
            }
            CreateRecurringEvent create = new CreateRecurringEvent
                (DateOnly.FromDateTime(Beginning), DateOnly.FromDateTime(Ending), Summary, Description, Attendees.Emails.Select(x => x.Label).ToImmutableArray(), CycleDays.Items.Where(item => item.IsSelected).Select(item => item.Label).ToImmutableArray(), Frequency, IsPublic, AllDay, TimeOnly.FromTimeSpan(StartTime), Duration);
            if (string.IsNullOrEmpty(Id))
            {
                var result = await _eventService.CreateNewEvent(create, OnError.DefaultBehavior(this));
                if (result.HasValue)
                {
                    Id = result.Value.id;
                    OnCreated?.Invoke(this, this);
                }
                Busy = false;
                return;
            }

            var result2 = await _eventService.UpdateEvent(Id, create, OnError.DefaultBehavior(this));
            if (result2.HasValue) { 
                OnUpdated?.Invoke(this, this);
            }
            Busy = false;
        }

        [RelayCommand]
        public async Task Delete()
        {
            Busy = true;
            await _eventService.DeleteEvent(Id, OnError.DefaultBehavior(this));
            OnDelete?.Invoke(this, this);
            Busy = false;
        }

        #region ISelectable stuff
        [ObservableProperty] bool isSelected;

        /// <summary>
        /// Do something if this Recurring Event is Selected.
        /// </summary>
        public event EventHandler<RecurringEventViewModel>? Selected;
        public event EventHandler<ErrorRecord>? OnError;

        [RelayCommand]
        public void Select()
        {
            IsSelected = !IsSelected;
            if (IsSelected)
                Selected?.Invoke(this, this);
        }

        #endregion // ISelectable

        #region ICachedViewModel stuff
        public static ConcurrentBag<RecurringEventViewModel> ViewModelCache { get; private set; } = [];

        public static RecurringEventViewModel Default => new();

        public static RecurringEventViewModel Get(CycleDayRecurringEvent model)
        {
            var vm = ViewModelCache.FirstOrDefault(re => re.Id == model.id);
            if (vm is not null)
                return vm.Clone();

            vm = new()
            {
                Id = model.id,
                Beginning = model.beginning.ToDateTime(default),
                Ending = model.ending.ToDateTime(default),
                CreatorId = model.creatorId,
                Summary = model.summary,
                Description = model.description,
                AllDay = model.allDay,
                StartTime = model.time.ToTimeSpan(),
                EndTime = model.time.AddMinutes(model.duration).ToTimeSpan(),
                CycleDays = new(),
                Frequency = model.frequency,
                IsPublic = model.isPublic,
                ShowDelete = true


                // TODO: Initialize the rest of the ObservableProperties you add.
            };
            vm.Attendees.AddEmails(model.attendees);

            foreach (var cycleday in model.cycleDays)
            {
                if (vm.CycleDays[cycleday] is not null)
                {
                    vm.CycleDays[cycleday]!.IsSelected = true;
                }
               
            }
            ViewModelCache.Add(vm);
            return vm.Clone();
        }

        public static List<RecurringEventViewModel> GetClonedViewModels(IEnumerable<CycleDayRecurringEvent> models)
        {
            List<RecurringEventViewModel> result = [];
            foreach(var model in models)
                result.Add(Get(model));

            return result;
        }

        public static async Task Initialize(CycleDayRecurringEventService service, Action<ErrorRecord> onError)
        {
            await service.WaitForInit(onError);
            ViewModelCache = [];
            _ = GetClonedViewModels(service.RecurringEvents);
        }

        /// <summary>
        /// Makes a shallow copy of this ViewModel.
        /// The point of this is to maintain clean copies in the ViewModelCache
        /// </summary>
        /// <returns></returns>
        public RecurringEventViewModel Clone() => (RecurringEventViewModel)MemberwiseClone();
        #endregion // ICachedViewModel

    }
}
