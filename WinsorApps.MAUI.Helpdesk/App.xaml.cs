namespace WinsorApps.MAUI.Helpdesk;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }
}