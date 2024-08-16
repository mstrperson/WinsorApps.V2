using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services.Admin;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels;

public partial class AdminCalendarViewModel :
    ObservableObject
{
    private readonly EventsAdminService _admin = ServiceHelper.GetService<EventsAdminService>();

    [ObservableProperty] CalendarViewModel calendar;
    

    public AdminCalendarViewModel()
    {
        calendar = new() { MonthlyEventSource = (month) => _admin.AllEvents.Values.Where(evt => evt.start.Month == month.Month) };
        _admin.OnCacheRefreshed += (_, _) => Calendar.LoadEvents();
    }

}

public partial class EventFilterViewModel :
    ObservableObject
{
    [ObservableProperty] StatusFilterViewModel byStatus = new();

}

public partial class TypeFilterViewModel :
    ObservableObject,
    IEventFormFilter
{
    [ObservableProperty]
    ObservableCollection<SelectableLabelViewModel> types =
    [
        new() { Label = EventType.Default },
        new() { Label = EventType.Rental },
        new() { Label = EventType.FieldTrip },
        new() { Label = EventType.VirtualEvent }
    ];

    public Func<EventFormViewModel, bool> Filter =>
        (evt) => Types.Where(s => s.IsSelected).Select(s => s.Label).Contains((string)evt.Type.Type);
}

public partial class StatusFilterViewModel :
    ObservableObject,
    IEventFormFilter
{
    [ObservableProperty]
    ObservableCollection<SelectableLabelViewModel> statuses =
        [
            new() { Label = ApprovalStatusLabel.Approved },
            new() { Label = ApprovalStatusLabel.Pending },
            new() { Label = ApprovalStatusLabel.RoomNotCleared },
            new() { Label = ApprovalStatusLabel.Declined },
            new() { Label = ApprovalStatusLabel.Withdrawn },
            new() { Label = ApprovalStatusLabel.Updating }
        ];

    public Func<EventFormViewModel, bool> Filter => 
        (evt) => Statuses.Where(s => s.IsSelected).Select(s => s.Label).Contains(evt.StatusSelection.Selected.Label);
}

public interface IEventFormFilter
{
    public Func<EventFormViewModel, bool> Filter { get; }
}