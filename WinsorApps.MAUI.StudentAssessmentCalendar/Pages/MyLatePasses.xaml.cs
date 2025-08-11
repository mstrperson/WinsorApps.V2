using WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.StudentAssessmentCalendar.Pages;

public partial class MyLatePasses : ContentPage
{
	public MyLatePasses(LatePassCollectionViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
        vm.LoadAssessmentRequested += Vm_LoadAssessmentRequested;
		BindingContext = vm;
		InitializeComponent();
	}

    private void Vm_LoadAssessmentRequested(object? sender, StudentAssessmentViewModel e)
    {
		var page = new AssessmentPage(e);
		Navigation.PushAsync(page);
    }
}