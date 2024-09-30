using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class StudentPage : ContentPage
{
	public StudentPageViewModel ViewModel => (StudentPageViewModel) BindingContext;
	public StudentPage(StudentPageViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		BindingContext = vm;
        vm.AssessmentSelected += Vm_AssessmentSelected;
		InitializeComponent();
	}

    private void Vm_AssessmentSelected(object? sender, AssessmentDetailsViewModel e)
    {
		var page = new AssessmentDetailPage(e);
		Navigation.PushAsync(page);
    }
}