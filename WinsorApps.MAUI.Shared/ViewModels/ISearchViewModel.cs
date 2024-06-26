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

public interface IMultiModalSearch<T> where T : ObservableObject
{
    ImmutableArray<string> SearchModes { get; }

    Func<T, bool> SearchFilter { get; }
}

public interface ICachedSearchViewModel<T> : IAsyncSearchViewModel<T> where T : ObservableObject, IEmptyViewModel<T>, new()
{
    public ImmutableArray<T> Available { get; set; }
    
    [RelayCommand]
    new public void Search();
}

public interface IAsyncSearchViewModel<T> where T : ObservableObject, IEmptyViewModel<T>, new()
{
    public ImmutableArray<T> AllSelected { get; set; }

    public ImmutableArray<T> Options { get; set; }

    public T Selected { get; set; }

    public SelectionMode SelectionMode { get; set; }

    public string SearchText { get; set; }

    public bool IsSelected { get; set; }

    public bool ShowOptions { get; set; }

    public event EventHandler<ImmutableArray<T>>? OnMultipleResult;
    public event EventHandler<T>? OnSingleResult;
    public event EventHandler? OnZeroResults;
    public void Select(T item);

    [RelayCommand]
    public void ClearSelection()
    {
        AllSelected = [];
        Options = [];
        Selected = IEmptyViewModel<T>.Empty;
        IsSelected = false;
        ShowOptions = false;
        SearchText = "";
    }

    [RelayCommand]
    public Task Search();
}
