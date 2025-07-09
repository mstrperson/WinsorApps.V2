using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class CalendarNotes : ContentPage
{
	public CalendarNotes(NotesPageViewModel vm)
	{
		this.BindingContext = vm;
		vm.Initialize().SafeFireAndForget(e => e.LogException());
		vm.OnError += this.DefaultOnErrorHandler();
        InitializeComponent();
	}
}