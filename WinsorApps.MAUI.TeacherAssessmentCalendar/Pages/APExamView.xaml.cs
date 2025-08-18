using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class APExamView : ContentPage
{
	public APExamView(APExamDetailViewModel viewModel)
	{
        BindingContext = viewModel;
        InitializeComponent();
	}
}