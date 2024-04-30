using Microsoft.Maui.Controls;

namespace WinsorApps.MAUI.Example;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }
}