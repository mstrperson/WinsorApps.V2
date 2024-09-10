using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class LateWorkPage : ContentPage
{
	public LateWorkPageViewModel ViewModel => (LateWorkPageViewModel)BindingContext;

	public LateWorkPage(LateWorkPageViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		BindingContext = vm;
		vm.Initialize(this.DefaultOnErrorAction()).SafeFireAndForget(e => e.LogException());
		InitializeComponent();
	}

    private void ToolbarItem_Clicked(object sender, EventArgs e)
    {
		if (!ViewModel.ShowSelectedSection)
			return;

		var page = new SectionLateWorkViewPage() { BindingContext = ViewModel.SelectedSection };
		Navigation.PushAsync(page);
    }
}