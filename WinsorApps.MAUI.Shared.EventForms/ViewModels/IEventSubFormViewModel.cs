﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public interface IEventSubFormViewModel<T, TModel>
    where T : ObservableObject
    where TModel : notnull
{
    public event EventHandler? ReadyToContinue;
    public event EventHandler? Deleted;

    public string Id { get; set; }

    public bool HasLoaded { get; set; }
    
    public void Load(TModel model);

    public void Clear();

    [RelayCommand]
    public abstract Task Continue(bool template = false);

    [RelayCommand]
    public abstract Task Delete();
}

