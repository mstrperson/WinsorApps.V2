using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class UserViewModel : ObservableObject
{
    private readonly RegistrarService _registrar;
    private readonly UserRecord _user;

    [ObservableProperty] private string displayName;
    public string Email => _user.email;

    [ObservableProperty] private ImmutableArray<SectionViewModel> academicSchedule = [];

    public event EventHandler<UserRecord>? Selected;

    public UserViewModel(UserRecord user)
    {
        _user = user;
        _registrar = ServiceHelper.GetService<RegistrarService>()!;
        displayName = _registrar.AllUsers.GetUniqueNameWithin(user);
    }


    [RelayCommand]
    public async Task LoadMySchedule()
    {
        var schedule = await _registrar.MyAcademicSchedule();
        AcademicSchedule = schedule
            .Select(sec => new SectionViewModel(sec))
            .ToImmutableArray();
    }

    [RelayCommand]
    public void Select()
    {
        Selected?.Invoke(this, _user);
    }
}