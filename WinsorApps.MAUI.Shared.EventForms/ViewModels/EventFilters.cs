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
    [ObservableProperty] private StatusFilterViewModel byStatus = new();
    [ObservableProperty] private bool showStatus = true;
    [ObservableProperty] private TypeFilterViewModel byType = new();
    [ObservableProperty] private bool showType = true;
    [ObservableProperty] private PersonSearchFilterViewModel byPerson = new();
    [ObservableProperty] private bool showPeople = true;
    [ObservableProperty] private EventNeedsFilterViewModel byNeed = new();
    [ObservableProperty] private bool showNeeds = true;

    public Func<EventFormViewModel, bool> Filter =>
        (evt) => ByStatus.Filter(evt)
              && ByType.Filter(evt)
              && ByPerson.Filter(evt)
              && ByNeed.Filter(evt);

    [RelayCommand]
    public void ClearFilter()
    {
        ByStatus.ClearFilter();
        ByType.ClearFilter();
        ByPerson.ClearFilter();
        ByNeed.ClearFilter();
    }

    [RelayCommand]
    public void ToggleShowNeeds()
    {
        ShowNeeds = !ShowNeeds;
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

public partial class EventNeedsFilterViewModel :
    ObservableObject,
    IEventFormFilter
{
    [ObservableProperty] private SelectableLabelViewModel facilities ="Facilities";
    [ObservableProperty] private SelectableLabelViewModel technology = "Technology";
    [ObservableProperty] private SelectableLabelViewModel theater = "Theater";
    [ObservableProperty] private SelectableLabelViewModel comms = "Comms";
    [ObservableProperty] private SelectableLabelViewModel catering =  "Catering";
    [ObservableProperty] private bool exclusive;
    public void ClearFilter()
    {
        Facilities.IsSelected = false;
    }

    [RelayCommand]
    public void ToggleExclusive() => Exclusive = !Exclusive;
    
    public Func<EventFormViewModel, bool> Filter => 
        (evt) =>
        Exclusive 
        ?   ((Facilities.IsSelected && evt.HasFacilities) || (!Facilities.IsSelected && !evt.HasFacilities))
        &&  ((Technology.IsSelected && evt.HasTech) || (!Technology.IsSelected && !evt.HasTech))
        &&  ((Theater.IsSelected && evt.HasTheater) || (!Theater.IsSelected && !evt.HasTheater))
        &&  ((Comms.IsSelected && evt.HasMarComm) || (!Comms.IsSelected && !evt.HasMarComm))
        &&  ((Catering.IsSelected && evt.HasCatering) || (!Catering.IsSelected && !evt.HasCatering))
        :   (!Facilities.IsSelected || evt.HasFacilities)
        &&  (!Technology.IsSelected || evt.HasTech)
        &&  (!Theater.IsSelected || evt.HasTheater)
        &&  (!Comms.IsSelected || evt.HasMarComm)
        &&  (!Catering.IsSelected || evt.HasCatering);
}

public partial class PersonSearchFilterViewModel :
    ObservableObject,
    IEventFormFilter
{
    [ObservableProperty] private string searchText = "";

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
    [ObservableProperty] private ObservableCollection<SelectableLabelViewModel> types =
    [
        new(EventType.Default),
        new(EventType.Rental),
        new(EventType.FieldTrip),
        new(EventType.VirtualEvent)
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
    [ObservableProperty] private ObservableCollection<SelectableLabelViewModel> statuses =
        [
            new(ApprovalStatusLabel.Approved),
            new(ApprovalStatusLabel.Pending),
            new(ApprovalStatusLabel.RoomNotCleared)
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