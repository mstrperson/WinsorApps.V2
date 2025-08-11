namespace WinsorApps.MAUI.Helpdesk.Pages;

public partial class CheckoutResultPage : ContentPage
{
	public CheckoutResultPage()
	{
		InitializeComponent();
		AutoPop().SafeFireAndForget(e => e.LogException());
	}

	private async Task AutoPop()
	{
		await Task.Delay(TimeSpan.FromSeconds(15));
		await Navigation.PopAsync();
	}
}