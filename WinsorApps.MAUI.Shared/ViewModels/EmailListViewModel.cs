using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace WinsorApps.MAUI.Shared.ViewModels
{
    public partial class EmailListViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<SelectableLabelViewModel> emails = [];
        [ObservableProperty] string emailEntry = "";

        [RelayCommand]
        public void AddEmail()
        {
            if (string.IsNullOrEmpty(EmailEntry))
            {
                return;
            }
            var email = new SelectableLabelViewModel() { Label = EmailEntry, IsSelected = true };
            email.Selected += (_, _) => Emails.Remove(email);
            Emails.Add(email);
            EmailEntry = "";
        }

        public void AddEmails(IEnumerable<string> e)
        {
            foreach (var email in e)
            {
                EmailEntry = email;
                AddEmail();
            }
        }
    }
}
