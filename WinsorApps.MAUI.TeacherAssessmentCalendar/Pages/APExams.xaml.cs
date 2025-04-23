using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class APExams : ContentPage
{
	public APExams(APExamPageViewModel viewModel)
	{
		BindingContext = viewModel;
		viewModel.OnError += this.DefaultOnErrorHandler();
        viewModel.CreateAPRequested += ViewModel_OnSelected;
        viewModel.OnSelected += ViewModel_OnSelected;
        viewModel.PopRequested += async (_, _) => await Navigation.PopAsync();
		InitializeComponent();
	}

    private void ViewModel_OnSelected(object? sender, APExamDetailViewModel e)
    {
        var page = new APExamEditor(e);
        Navigation.PushAsync(page);
    }

}