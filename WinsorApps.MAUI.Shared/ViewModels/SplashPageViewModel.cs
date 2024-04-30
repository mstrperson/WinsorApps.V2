using System;
using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinsorApps.MAUI.Shared.ViewModels;

public partial class SplashPageViewModel : ObservableObject
{
    [ObservableProperty] string title;

    [ObservableProperty] string subTitle;

    [ObservableProperty] ImmutableArray<string> messages;

    [ObservableProperty] bool isCaptive;

    [ObservableProperty] TimeSpan timeout;

    public SplashPageViewModel(string title, ImmutableArray<string>? startingMessages = null, TimeSpan? timeout = null)
    {
        this.title = title;
        subTitle = "";
        messages = startingMessages ?? [];
        isCaptive = true;
        this.timeout = timeout ?? TimeSpan.Zero;
    }
}