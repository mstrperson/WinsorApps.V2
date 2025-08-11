using WinsorApps.MAUI.StudentAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.StudentAssessmentCalendar.Pages;

public partial class LatePassRequest : ContentPage
{
	public LatePassRequest(StudentAssessmentViewModel vm)
	{
		this.BindingContext = vm;
		vm.LatePass.FreeBlockLookup.LoadFreeBlocks().SafeFireAndForget(e => e.LogException());
		InitializeComponent();
	}
}