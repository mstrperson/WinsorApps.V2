using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;
using WinsorApps.Services.Global;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class MyAssessmentsPage : ContentPage
{
	public MyAssessmentsPageViewModel ViewModel => (MyAssessmentsPageViewModel)BindingContext;

	public MyAssessmentsPage(MyAssessmentsPageViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();

		BindingContext = vm;

		vm.PageRequested += (_, page) => Navigation.PushAsync(page);
		InitializeComponent();
		this.Appearing += (_, _) => vm.Refresh().SafeFireAndForget(e => e.LogException());
	}


    private void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        foreach (var c in ViewModel.MyCourses)
        {
            c.IsSelected = false;
        }

        if (sender is Picker picker && picker.SelectedItem is CourseViewModel course)
		{
			course.Select();
		}
    }
}