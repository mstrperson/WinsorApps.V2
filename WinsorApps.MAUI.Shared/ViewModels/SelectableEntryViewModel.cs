using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinsorApps.MAUI.Shared.ViewModels
{
    public partial class SelectableEntryViewModel<T> : 
        ObservableObject, 
        ISelectable<SelectableEntryViewModel<T>>
    {
        [ObservableProperty] T value;
        [ObservableProperty] bool isSelected;

        public event EventHandler<SelectableEntryViewModel<T>>? Selected;

        public SelectableEntryViewModel(T value)
        {
            Value = value;
        }


        [RelayCommand]
        public void Select()
        {
            IsSelected = !IsSelected;
            if (IsSelected)
                Selected?.Invoke(this, this);
        }

        public static implicit operator SelectableEntryViewModel<T>(T value) => new(value);
        public static implicit operator T(SelectableEntryViewModel<T> value) => value.Value;
    }
}
