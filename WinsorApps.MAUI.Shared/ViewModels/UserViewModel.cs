using System.Collections.Immutable;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class UserViewModel : ObservableObject, IEmptyViewModel<UserViewModel>, ISelectable<UserViewModel>, IErrorHandling
{
    private readonly RegistrarService _registrar;
    public readonly UserRecord User;

    public string BlackbaudId => $"{User.blackbaudId}";
    public string Id => User.id;
    
    [ObservableProperty] private string displayName;
    public string Email => User.email;

    [ObservableProperty] private ImmutableArray<SectionViewModel> academicSchedule = [];
    [ObservableProperty] private bool showButton = false;

    public event EventHandler<UserViewModel>? Selected;
    public event EventHandler<SectionViewModel>? SectionSelected;
    public event EventHandler<ErrorRecord>? OnError;

    public UserViewModel()
    {
        _registrar = ServiceHelper.GetService<RegistrarService>()!;
        User = new();
        displayName = "";
    }
    public UserViewModel(UserRecord user)
    {
        User = user;
        displayName = $"{user.firstName} {user.lastName}";
        _registrar = ServiceHelper.GetService<RegistrarService>()!;
        
    }


    [RelayCommand]
    public void GetUniqueDisplayName()
    {
        try
        {
            DisplayName = _registrar.GetUniqueDisplayNameFor(User);
        }
        catch (ServiceNotReadyException)
        {
            // ignore
        }
    }


    [RelayCommand]
    public void LoadMySchedule()
    {
        var schedule = _registrar.MyAcademicSchedule;
        AcademicSchedule = schedule
            .Select(sec => new SectionViewModel(sec))
            .ToImmutableArray();
        foreach (var section in AcademicSchedule)
            section.Selected += (sender, sec) => SectionSelected?.Invoke(sender, sec); 
        ShowButton = false;
    }
    
    [RelayCommand]
    public void Select()
    {
        Selected?.Invoke(this, this);
    }
}