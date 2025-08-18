using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.CDRE.ViewModels
{
    public partial class CycleDaySelectionViewModel : ObservableObject, ICheckBoxListViewModel<SelectableLabelViewModel>
    {
        [ObservableProperty]
        private ObservableCollection<SelectableLabelViewModel> items = [];

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
                "Day 1",
                "Day 2",
                "Day 3",
                "Day 4",
                "Day 5",
                "Day 6",
                "Day 7",
            ];
        }
    }

    public partial class EventListViewModel: ObservableObject, IErrorHandling
    {
        private readonly CycleDayRecurringEventService _eventService = ServiceHelper.GetService<CycleDayRecurringEventService>();
        private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

        [ObservableProperty] private List<RecurringEventViewModel> events = [];
        public event EventHandler<RecurringEventViewModel>? CreateRequested;
        public event EventHandler<RecurringEventViewModel>? Reload;
        public event EventHandler<RecurringEventViewModel>? EditRequested;
        public event EventHandler<ErrorRecord>? OnError;

        [ObservableProperty] private bool isBusy;

        [RelayCommand]
        public void LoadEvents()
        {
            IsBusy = true;
            Events = [.. RecurringEventViewModel.GetClonedViewModels(_eventService.OpenEventList)];
            foreach (var evt in Events)
            {
                evt.OnError += (sender, e) => OnError?.Invoke(sender, e);
                evt.Selected += (sender, e) => EditRequested?.Invoke(sender, e);
                evt.OnCreated += (sender, e) =>
                {
                    Events.Add(e);
                    Reload?.Invoke(sender, e);
                };
                evt.OnUpdated += (sender, e) =>
                {
                    Events.Replace(evt, e);
                    Reload?.Invoke(sender, e);
                };
                evt.OnDelete += (sender, e) =>
                {
                    Events.Remove(evt);
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
                Events.Add(e);
                Reload?.Invoke(sender, e);
            };
            evt.OnUpdated += (sender, e) =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{e.Summary} {e.Id} updated");
                Events.Replace(evt, e);
                Reload?.Invoke(sender, e);
            };
            evt.OnDelete += (sender, e) =>
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"{e.Summary} {e.Id} deleted");
                Events.Remove(evt);
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

        [ObservableProperty] private string id = "";
        [ObservableProperty] private DateTime beginning = DateTime.Today;
        [ObservableProperty] private DateTime ending = DateTime.Today;
        [ObservableProperty] private string creatorId = "";
        [ObservableProperty] private string summary = "";
        [ObservableProperty] private string description = "";
        [ObservableProperty] private EmailListViewModel attendees = new();
        [ObservableProperty] private bool allDay;
        [ObservableProperty] private TimeSpan startTime;
        [ObservableProperty] private TimeSpan endTime;
        [ObservableProperty] private CycleDaySelectionViewModel cycleDays = new();
        [ObservableProperty] private int frequency = 1;
        [ObservableProperty] private bool isPublic;
        [ObservableProperty] private bool showDelete = false;
        [ObservableProperty] private ObservableCollection<SelectableLabelViewModel> frequencyOptions = ["1", "2", "3", "4", "5", "6", "7", "8", "9"];
        [ObservableProperty] private bool showFrequencyOptions;
        [ObservableProperty] private bool busy;
        [ObservableProperty] private string busyMessage = "Loading";
       
        public int Duration => (int)(EndTime - StartTime).TotalMinutes;

        public RecurringEventViewModel()
        {
            foreach (var entry in FrequencyOptions)
                entry.Selected += (_, _) =>
                {
                    Frequency = int.Parse(entry.Label);
                    ShowFrequencyOptions = false;
                };
        }
        
        public event EventHandler<RecurringEventViewModel>? OnCreated;
        public event EventHandler<RecurringEventViewModel>? OnUpdated;
        public event EventHandler<RecurringEventViewModel>? OnDelete;
        public event EventHandler<RecurringEventViewModel>? EditRequested;

        [RelayCommand]
        public void ToggleShowFreqOpts() => ShowFrequencyOptions = !ShowFrequencyOptions;
        
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
            CreateRecurringEvent create = new                (DateOnly.FromDateTime(Beginning), DateOnly.FromDateTime(Ending), Summary, Description, [.. Attendees.Emails.Select(x => x.Label)], [.. CycleDays.Items.Where(item => item.IsSelected).Select(item => item.Label)], Frequency, IsPublic, AllDay, TimeOnly.FromTimeSpan(StartTime), Duration);
            if (string.IsNullOrEmpty(Id))
            {
                var result = await _eventService.CreateNewEvent(create, OnError.DefaultBehavior(this));
                if (result is not null)
                {
                    Id = result.id;
                    OnCreated?.Invoke(this, this);
                }
                Busy = false;
                return;
            }

            var result2 = await _eventService.UpdateEvent(Id, create, OnError.DefaultBehavior(this));
            if (result2 is not null) { 
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
        [ObservableProperty] private bool isSelected;

        /// <summary>
        /// Do something if this Recurring Event is Selected.
        /// </summary>
        public event EventHandler<RecurringEventViewModel>? Selected;
        public event EventHandler<ErrorRecord>? OnError;

        [RelayCommand]
        public void Select()
        {
            IsSelected = !IsSelected;
            Selected?.Invoke(this, this);
        }

        #endregion // ISelectable

        #region ICachedViewModel stuff
        public static List<RecurringEventViewModel> ViewModelCache { get; private set; } = [];

        public static RecurringEventViewModel Empty => new();

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
