using CommunityToolkit.Mvvm.ComponentModel;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.MAUI.Shared.ViewModels;

public interface IBusyViewModel
{
    public bool Busy { get; set; }
    public string BusyMessage { get; set; }

    public void BusyChangedCascade(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is IBusyViewModel busyBee)
        {
            if (e.PropertyName == "BusyMessage")
            {
                this.BusyMessage = busyBee.BusyMessage;
            }

            if (e.PropertyName == "Busy")
            {
                Busy = busyBee.Busy;
            }
        }
    }
}

public interface IDefaultValueViewModel<T> where T : ObservableObject
{
    public static abstract T Empty { get; }
}
public interface ISelectable<T> where T : ObservableObject
{
    public event EventHandler<T>? Selected;

    public bool IsSelected { get; set; }

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
