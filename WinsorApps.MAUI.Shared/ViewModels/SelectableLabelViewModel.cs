using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
