using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WinsorApps.Services.Global;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class MonthPickerViewModel :
    ObservableObject
{
    [ObservableProperty] ObservableCollection<MonthSelectionViewModel> months = 
    [ ..
        new DateRange(DateTime.Today.MonthOf().AddYears(-1), DateTime.Today.MonthOf().AddYears(1))
            .Where(dt => dt.Day == 1)
            .Select(dt => dt.ToDateTime(default))
    ];
    
    [ObservableProperty] MonthSelectionViewModel selectedMonth= DateTime.Today.MonthOf();

    public MonthPickerViewModel()
    {
        foreach (var month in Months)
        {
            month.Selected += (_, e) => selectedMonth = e;
        }
    }
}

public partial class MonthSelectionViewModel :
    ObservableObject,
    ISelectable<MonthSelectionViewModel>
{
    [ObservableProperty] private DateTime selectedDate;
    [ObservableProperty] private bool isSelected;
    
    public static implicit operator MonthSelectionViewModel(DateTime date) => new(date);
    
    public override string ToString() => $"{SelectedDate:MMMM yyyy}";
    
    public MonthSelectionViewModel()
    {
        SelectedDate = DateTime.Today.MonthOf();
    }

    public MonthSelectionViewModel(DateTime date)
    {
        SelectedDate = date.MonthOf();
    }

    public event EventHandler<MonthSelectionViewModel>? Selected;
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}