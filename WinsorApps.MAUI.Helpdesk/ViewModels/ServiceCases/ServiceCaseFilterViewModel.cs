using WinsorApps.MAUI.Helpdesk.ViewModels.Devices;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Helpdesk.Models;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.ServiceCases
{
    public partial class ServiceCaseFilterViewModel : ObservableObject
    {
        [ObservableProperty] private SelectableEntryViewModel<bool> open = true;
        [ObservableProperty] private SelectableEntryViewModel<ServiceStatusSearchViewModel> status = new ServiceStatusSearchViewModel();
        [ObservableProperty] private SelectableEntryViewModel<DeviceSearchViewModel> device = new DeviceSearchViewModel();
        [ObservableProperty] private SelectableEntryViewModel<UserSearchViewModel> owner = new UserSearchViewModel();
        [ObservableProperty] private SelectableEntryViewModel<DateTime> start = DateTime.Today.AddDays(-7);
        [ObservableProperty] private SelectableEntryViewModel<DateTime> end = DateTime.Today.AddDays(1);

        public ServiceCaseFilter Filter => new(
            Open.IsSelected ? Open.Value : null,
            Status.IsSelected ? Status.Value.Selected.Status : null,
            Device.IsSelected ? Device.Value.Selected.Id : null,
            Owner.IsSelected ? Owner.Value.Selected.Id : null,
            Start.IsSelected ? Start.Value : default,
            End.IsSelected ? End.Value : default);

        public static implicit operator ServiceCaseFilter(ServiceCaseFilterViewModel vm) => vm.Filter;
    }
}
