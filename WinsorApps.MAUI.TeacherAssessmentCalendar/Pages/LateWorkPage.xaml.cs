using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class LateWorkPage : ContentPage
{
	public LateWorkPage(LateWorkPageViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		BindingContext = vm;
		vm.Initialize(this.DefaultOnErrorAction()).SafeFireAndForget(e => e.LogException());
		InitializeComponent();
	}
}