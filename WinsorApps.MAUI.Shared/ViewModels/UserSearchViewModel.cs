using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class UserSearchViewModel : ObservableObject, ICachedSearchViewModel<UserViewModel>, IErrorHandling
{
    [ObservableProperty] private ImmutableArray<UserViewModel> available;
    [ObservableProperty] private ImmutableArray<UserViewModel> allSelected = [];
    [ObservableProperty] private ImmutableArray<UserViewModel> options = [];
    [ObservableProperty] private SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty] private UserViewModel selected;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool showOptions;

    public event EventHandler<ImmutableArray<UserViewModel>>? OnMultipleResult;
    public event EventHandler<UserViewModel>? OnSingleResult;
    public event EventHandler<ErrorRecord>? OnError;

    public UserSearchViewModel()
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        isSelected = false;
        selected = IEmptyViewModel<UserViewModel>.Empty;
        available = registrar.AllUsers.Select(u => new UserViewModel(u)).ToImmutableArray();
        foreach(var user in Available)
            user.Selected += UserOnSelected;
    }

    [RelayCommand]
    public void ClearSelection()
    {
        AllSelected = [];
        Options = [];
        Selected = IEmptyViewModel<UserViewModel>.Empty;
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
                Selected = Available.FirstOrDefault(user => user.Id == selectedUser.Id) ?? IEmptyViewModel<UserViewModel>.Empty;
                IsSelected = string.IsNullOrEmpty(Selected.Id);
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

                IsSelected = AllSelected.Length > 0;
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    [RelayCommand]
    public void Search()
    {
        var possible = Available
            .Where(user => 
                user.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)
                || user.BlackbaudId == SearchText);
        switch (SelectionMode)
        {
            case SelectionMode.Multiple:
                AllSelected = [..possible];
                IsSelected = AllSelected.Length > 0;
                OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.Single:
                Options = [..possible];
                if (Options.Length == 0)
                {
                    ShowOptions = false;
                    Selected = IEmptyViewModel<UserViewModel>.Empty;
                    IsSelected = false;
                    return;
                }

                if (Options.Length == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    SearchText = Selected.DisplayName;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = IEmptyViewModel<UserViewModel>.Empty;
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