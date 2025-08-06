using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinsorApps.MAUI.Shared.ViewModels
{
    public partial class SelectableLabelViewModel : ObservableObject, ISelectable<SelectableLabelViewModel>
    {
        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string label = "";

        public event EventHandler<SelectableLabelViewModel>? Selected;

        [RelayCommand]
        public void Select()
        {
            IsSelected = !IsSelected;
            Selected?.Invoke(this, this);
        }

        public SelectableLabelViewModel(string label)
        {
            Label = label;
        }

        public static implicit operator SelectableLabelViewModel(string label) => new(label);
    }
}
