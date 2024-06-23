using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
