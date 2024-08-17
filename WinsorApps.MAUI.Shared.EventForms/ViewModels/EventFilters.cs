using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class EventFilterViewModel :
    ObservableObject,
    IEventFormFilter
{
    [ObservableProperty] StatusFilterViewModel byStatus = new();
    [ObservableProperty] bool showStatus;
    [ObservableProperty] TypeFilterViewModel byType = new();
    [ObservableProperty] bool showType;
    [ObservableProperty] PersonSearchFilterViewModel byPerson = new();
    [ObservableProperty] bool showPeople;

    public Func<EventFormViewModel, bool> Filter =>
        (evt) => ByStatus.Filter(evt)
              && ByType.Filter(evt)
              && ByPerson.Filter(evt);

    [RelayCommand]
    public void ClearFilter()
    {
        ByStatus.ClearFilter();
        ByType.ClearFilter();
        ByPerson.ClearFilter();
    }

    [RelayCommand]
    public void ToggleShowStatus()
    {
        ShowStatus = !ShowStatus;
    }
    [RelayCommand]
    public void ToggleShowType()
    {
        ShowType = !ShowType;
    }
    [RelayCommand]
    public void ToggleShowPeople()
    {
        ShowPeople = !ShowPeople;
    }

}

public partial class PersonSearchFilterViewModel :
    ObservableObject,
    IEventFormFilter
{
    [ObservableProperty] string searchText = "";

    public Func<EventFormViewModel, bool> Filter =>
        evt => evt.Leader.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)
            || evt.Creator.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase);

    [RelayCommand]
    public void ClearFilter()
    {
        SearchText = "";
    }
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
        (evt) => Types.Where(s => s.IsSelected).Count() switch
        {
            0 => true,
            _ => Types.Where(s => s.IsSelected).Select(s => s.Label).Contains((string)evt.Type.Type)
        };

    [RelayCommand]
    public void ClearFilter()
    {
        foreach (var type in Types)
            type.IsSelected = false;
    }
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
            new() { Label = ApprovalStatusLabel.RoomNotCleared }
        ];

    public Func<EventFormViewModel, bool> Filter =>
        (evt) => Statuses.Where(s => s.IsSelected).Count() switch
        {
            0 => true,
            _ => Statuses.Where(s => s.IsSelected).Select(s => s.Label).Contains(evt.StatusSelection.Selected.Label)
        };

    [RelayCommand]
    public void ClearFilter()
    {
        foreach (var status in Statuses)
            status.IsSelected = false;
    }
}

public interface IEventFormFilter
{
    public void ClearFilter();
    public Func<EventFormViewModel, bool> Filter { get; }
}