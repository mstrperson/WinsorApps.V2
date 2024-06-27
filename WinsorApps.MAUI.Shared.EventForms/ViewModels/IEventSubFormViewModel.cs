using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels
{
    public interface IEventSubFormViewModel
    {
        public event EventHandler? ReadyToContinue;
        public event EventHandler? Deleted;


        [RelayCommand]
        public abstract Task Continue();

        [RelayCommand]
        public abstract Task Delete();
    }
}
