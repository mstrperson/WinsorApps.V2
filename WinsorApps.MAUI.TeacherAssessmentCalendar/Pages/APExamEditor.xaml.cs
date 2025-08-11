using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class APExamEditor : ContentPage
{
	public APExamEditor(APExamDetailViewModel viewModel)
	{
        BindingContext = viewModel;
        InitializeComponent();
	}
}