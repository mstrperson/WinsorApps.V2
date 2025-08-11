using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace WinsorApps.MAUI.Shared.ViewModels;

public interface ICheckBoxListViewModel<T> where T:ObservableObject, ISelectable<T>
{
    public ObservableCollection<T> Items { get; }

    public ObservableCollection<T> Selected => [ .. Items.Where(item => item.IsSelected)];
}
