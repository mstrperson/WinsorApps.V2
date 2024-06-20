using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        ISelectable<RecurringEventViewModel>
    {
        private readonly CycleDayRecurringEventService _eventService = ServiceHelper.GetService<CycleDayRecurringEventService>();

        [ObservableProperty] string id = "";

        // TODO:  Add More Observable Properties for all the relevant
        //        things for a CycleDayRecurringEvent

        private RecurringEventViewModel()
        {
            
        }


        #region ISelectable stuff
        [ObservableProperty] bool isSelected;

        /// <summary>
        /// Do something if this Recurring Event is Selected.
        /// </summary>
        public event EventHandler<RecurringEventViewModel>? Selected;

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

        public static RecurringEventViewModel Get(CycleDayRecurringEvent model)
        {
            var vm = ViewModelCache.First(re => re.Id == model.id);
            if (vm is not null)
                return vm.Clone();

            vm = new()
            {
                Id = model.id,
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

    }
}
