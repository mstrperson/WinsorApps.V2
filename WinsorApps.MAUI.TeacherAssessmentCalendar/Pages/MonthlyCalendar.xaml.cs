using AsyncAwaitBestPractices;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class MonthlyCalendar : ContentPage
{
	public MonthlyCalendar(MonthlyCalendarViewModel vm)
	{
		vm.OnError += this.DefaultOnErrorHandler();
		BindingContext = vm;
        vm.DaySelected += Vm_DaySelected;
        vm.EventSelected += Vm_EventSelected;
		InitializeComponent();
	}

    private void Vm_EventSelected(object? sender, Shared.AssessmentCalendar.ViewModels.AssessmentCalendarEventViewModel e)
    {
        var model = e.Model.Reduce(AssessmentCalendarEvent.Empty);
        if (model.type == AssessmentType.Assessment)
        {
            var page = new AssessmentDetailPage(AssessmentDetailsViewModel.Get(model));
            Navigation.PushAsync(page);
        }
        if(model.type == AssessmentType.ApExam)
        {
            var vm = new APExamDetailViewModel();
            vm.LoadFromEventData(model).SafeFireAndForget(e => e.LogException());
            var page = new APExamView(vm);
            Navigation.PushAsync(page);
        }
    }

    private void Vm_DaySelected(object? sender, Shared.AssessmentCalendar.ViewModels.CalendarDayViewModel e)
    {
		var page = new CalendarDayPage(e);
		Navigation.PushAsync(page);
    }
}