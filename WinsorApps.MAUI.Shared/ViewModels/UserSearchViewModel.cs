using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class UserSearchViewModel : ObservableObject
{
    [ObservableProperty] private ImmutableArray<UserViewModel> availableUsers;
    [ObservableProperty] private ImmutableArray<UserViewModel> selectedUsers = [];
    [ObservableProperty] private ImmutableArray<UserViewModel> options = [];
    [ObservableProperty] private SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty] private UserViewModel selectedUser;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool showOptions;

    public event EventHandler<ImmutableArray<UserViewModel>>? OnMultipleResult;
    public event EventHandler<UserViewModel>? OnSingleResult;

    public UserSearchViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        isSelected = false;
        selectedUser = UserViewModel.Empty;
        availableUsers = registrar.AllUsers.Select(u => new UserViewModel(u)).ToImmutableArray();
        foreach(var user in AvailableUsers)
            user.Selected += UserOnSelected;
    }

    public void SelectUser(UserViewModel user)
    {
        UserOnSelected(this, user.User);
    }
    
    private void UserOnSelected(object? sender, UserRecord selectedUser)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                SelectedUser = AvailableUsers.FirstOrDefault(user => user.Id == selectedUser.id) ?? UserViewModel.Empty;
                IsSelected = string.IsNullOrEmpty(SelectedUser.Id);
                Options = [];
                ShowOptions = false;
                OnSingleResult?.Invoke(this, SelectedUser);
                return;
            case SelectionMode.Multiple:
                var user = AvailableUsers.FirstOrDefault(user => user.Id == selectedUser.id);
                if (user is null) return;
                if (SelectedUsers.Contains(user))
                    SelectedUsers = [.. SelectedUsers.Except([user])];
                else
                    SelectedUsers = [..SelectedUsers, user];

                IsSelected = SelectedUsers.Length > 0;
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedUsers = [];
        Options = [];
        SelectedUser = UserViewModel.Empty;
        IsSelected = false;
        ShowOptions = false;
    }

    [RelayCommand]
    public void Search()
    {
        var possible = AvailableUsers
            .Where(user => 
                user.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)
                || user.BlackbaudId == SearchText);
        switch (SelectionMode)
        {
            case SelectionMode.Multiple:
                SelectedUsers = [..possible];
                IsSelected = SelectedUsers.Length > 0;
                OnMultipleResult?.Invoke(this, SelectedUsers);
                return;
            case SelectionMode.Single:
                Options = [..possible];
                if (Options.Length == 0)
                {
                    ShowOptions = false;
                    SelectedUser = UserViewModel.Empty;
                    IsSelected = false;
                    return;
                }

                if (Options.Length == 1)
                {
                    ShowOptions = false;
                    SelectedUser = Options.First();
                    IsSelected = true;
                    OnSingleResult?.Invoke(this, SelectedUser);
                    return;
                }

                ShowOptions = true;
                SelectedUser = UserViewModel.Empty;
                IsSelected = false;
                return;
            default: return;
        }
    }
}