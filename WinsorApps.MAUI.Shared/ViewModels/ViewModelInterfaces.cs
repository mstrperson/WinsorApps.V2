﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

public interface IBusyViewModel
{
    public bool Busy { get; }
    public string BusyMessage { get; }
}

public interface IDefaultValueViewModel<T> where T: ObservableObject
{
    public static abstract T Default { get; }
}
public interface ISelectable<T> where T : ObservableObject
{
    public event EventHandler<T>? Selected;

    public bool IsSelected { get; set; }

    [RelayCommand]
    public void Select();
}

public interface IErrorHandling
{
    public event EventHandler<ErrorRecord>? OnError;
}

public interface ILinkedList<T> where T : ObservableObject
{
    public T Next();
}

public interface IAsyncSubmit
{
    public Task Submit();

    public bool Working { get; set; }
}
