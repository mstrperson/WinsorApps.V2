using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class UserSearchViewModel : 
    ObservableObject, 
    ICachedSearchViewModel<UserViewModel>, 
    IErrorHandling
{
    [ObservableProperty] private ObservableCollection<UserViewModel> available = [];
    [ObservableProperty] private ObservableCollection<UserViewModel> allSelected = [];
    [ObservableProperty] private ObservableCollection<UserViewModel> options = [];
    [ObservableProperty] private SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty] private UserViewModel selected;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool showOptions;

    public event EventHandler<ObservableCollection<UserViewModel>>? OnMultipleResult;
    public event EventHandler<UserViewModel>? OnSingleResult;
    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? OnZeroResults;

    public UserSearchViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        isSelected = false;
        selected = UserViewModel.Empty;
        if (registrar.Ready)
        {
            Available = [.. UserViewModel.GetClonedViewModels(registrar.AllUsers)];
            foreach (var user in Available)
                user.Selected += UserOnSelected;
        }
    }

    public void SetAvailableUsers(IEnumerable<UserRecord> users)
    {
        ClearSelection();
        Available = [..UserViewModel.GetClonedViewModels(users)];
        foreach (var user in Available)
            user.Selected += UserOnSelected;
    }

    [RelayCommand]
    public void ClearSelection()
    {
        AllSelected = [];
        Options = [];
        Selected = UserViewModel.Empty;
        IsSelected = false;
        ShowOptions = false;
        SearchText = "";
    }

    public void Select(UserViewModel user)
    {
        UserOnSelected(this, user);
    }
    
    private void UserOnSelected(object? sender, UserViewModel selectedUser)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                Selected = Available.FirstOrDefault(user => user.Id == selectedUser.Id) ?? UserViewModel.Empty;
                if (Selected.Id != "")
                {
                    foreach (var item in Available.Except([Selected]))
                    {
                        item.IsSelected = false;
                    }
                    Selected.IsSelected = true;
                }
                IsSelected = true;
                Options = [];
                ShowOptions = false;
                SearchText = Selected.DisplayName;
                OnSingleResult?.Invoke(this, Selected);
                return;
            case SelectionMode.Multiple:
                var user = Available.FirstOrDefault(user => user.Id == selectedUser.Id);
                if (user is null) return;
                if (AllSelected.Contains(user))
                    AllSelected = [.. AllSelected.Except([user])];
                else
                    AllSelected = [.. AllSelected, user];
                
                IsSelected = AllSelected.Count > 0;
                if (AllSelected.Count > 0)
                {
                    foreach (var item in Available)
                    {
                        item.IsSelected = AllSelected.Contains(item);
                    }
                }
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    [RelayCommand]
    public void Search()
    {
        SearchText = SearchText.Trim();

        var possible = Available
            .Where(user => 
                user.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)
                || user.BlackbaudId == SearchText);

        if (!possible.Any())
            OnZeroResults?.Invoke(this, EventArgs.Empty);

        switch (SelectionMode)
        {
            case SelectionMode.Multiple:
                AllSelected = [..possible];
                IsSelected = AllSelected.Count > 0;
                OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.Single:
                Options = [..possible];
                if (Options.Count == 0)
                {
                    ShowOptions = false;
                    Selected = UserViewModel.Empty;
                    IsSelected = false;
                    return;
                }

                if (Options.Count == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    SearchText = Selected.DisplayName;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = UserViewModel.Empty;
                IsSelected = false;
                return;
            default: return;
        }
    }

    async Task IAsyncSearchViewModel<UserViewModel>.Search()
    {
        await Task.Run(Search);
    }
}