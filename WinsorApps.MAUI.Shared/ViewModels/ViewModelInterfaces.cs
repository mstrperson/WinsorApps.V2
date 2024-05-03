using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.ViewModels;

public interface IEmptyViewModel<T> where T : ObservableObject, new()
{
    public static T Empty = new();

    public static T CreateBlank() => new();
}

public interface ISelectable<T> where T : ObservableObject
{
    public event EventHandler<T>? Selected;

    [RelayCommand]
    public void Select();
}

public interface IErrorHandling
{
    public event EventHandler<ErrorRecord>? OnError;
}