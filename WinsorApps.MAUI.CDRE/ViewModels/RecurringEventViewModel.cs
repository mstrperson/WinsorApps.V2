using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.PlatformConfiguration.TizenSpecific;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.CDRE.ViewModels
{
    public partial class RecurringEventViewModel :
        ObservableObject,
        ICachedViewModel<RecurringEventViewModel, CycleDayRecurringEvent, CycleDayRecurringEventService>,
        ISelectable<RecurringEventViewModel>,
        IDefaultValueViewModel<RecurringEventViewModel>,
        IErrorHandling
    {
        private readonly CycleDayRecurringEventService _eventService = ServiceHelper.GetService<CycleDayRecurringEventService>();

        [ObservableProperty] string id = "";
        [ObservableProperty] DateOnly beginning;
        [ObservableProperty] DateOnly ending;
        [ObservableProperty] string creatorId = "";
        [ObservableProperty] string summary = "";
        [ObservableProperty] string description = "";
        [ObservableProperty] ImmutableArray<string> attendees;
        [ObservableProperty] bool allDay;
        [ObservableProperty] TimeOnly startTime;
        [ObservableProperty] TimeOnly endTime;
        [ObservableProperty] ImmutableArray<string> cycleDays;
        [ObservableProperty] int frequency;
        [ObservableProperty] bool isPublic;
        public int Duration => (int)(EndTime - StartTime).TotalMinutes;

        // TODO:  Add More Observable Properties for all the relevant
        //        things for a CycleDayRecurringEvent

        private RecurringEventViewModel()
        {
            
        }

        [RelayCommand]
        public async Task Submit()
        {
            CreateRecurringEvent create = new CreateRecurringEvent
                (Beginning, Ending, Summary, Description, Attendees, CycleDays, Frequency, IsPublic, AllDay, StartTime, Duration);
            if (string.IsNullOrEmpty(Id))
            {
                var result = await _eventService.CreateNewEvent(create, OnError.DefaultBehavior(this));
                if (result.HasValue)
                {
                    Id = result.Value.id;
                }
                return;
            }

            await _eventService.UpdateEvent(Id, create, OnError.DefaultBehavior(this));
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
            var vm = ViewModelCache.First(re => re.Id == model.id);
            if (vm is not null)
                return vm.Clone();

            vm = new()
            {
                Id = model.id,
                Beginning=model.beginning,
                Ending=model.ending,
                CreatorId=model.creatorId,
                Summary=model.summary,
                Description=model.description,
                Attendees=model.attendees,
                AllDay=model.allDay,
                StartTime=model.time,
                EndTime=model.time.AddMinutes(model.duration),
                CycleDays=model.cycleDays,
                Frequency=model.frequency,
                IsPublic=model.isPublic,


                // TODO: Initialize the rest of the ObservableProperties you add.
            };
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
