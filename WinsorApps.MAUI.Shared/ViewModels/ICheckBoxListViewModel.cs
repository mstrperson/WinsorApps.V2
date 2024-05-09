using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinsorApps.MAUI.Shared.ViewModels;

public interface ICheckBoxListViewModel<T> where T:ObservableObject, ISelectable<T>
{
    public ImmutableArray<T> Items { get; }

    public ImmutableArray<T> Selected => [ .. Items.Where(item => item.IsSelected)];
}
