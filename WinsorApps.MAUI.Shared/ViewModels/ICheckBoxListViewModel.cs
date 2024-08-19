using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Immutable;

namespace WinsorApps.MAUI.Shared.ViewModels;

public interface ICheckBoxListViewModel<T> where T:ObservableObject, ISelectable<T>
{
    public ImmutableArray<T> Items { get; }

    public ImmutableArray<T> Selected => [ .. Items.Where(item => item.IsSelected)];
}
