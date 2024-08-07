using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.AssessmentCalendar.ViewModels
{
    public partial class LatePassViewModel :
        ObservableObject,
        IModelCarrier<LatePassViewModel, AssessmentPassDetail>
    {
        [ObservableProperty] string courseName = "";
        [ObservableProperty] string note = "";
        [ObservableProperty] DateTime dateAndTime;
        [ObservableProperty] DateTime timestamp;
        [ObservableProperty] UserViewModel student = UserViewModel.Empty;
        public OptionalStruct<AssessmentPassDetail> Model { get; private set; } = OptionalStruct<AssessmentPassDetail>.None();

        public event EventHandler<AssessmentPassDetail>? LoadAssessmentRequested;

        protected LatePassViewModel() { }

        public static ObservableCollection<LatePassViewModel> GetPasses(AssessmentEntryRecord assessment)
        {
            var registrar = ServiceHelper.GetService<RegistrarService>();
            ObservableCollection<LatePassViewModel> passes = [];
            foreach(var latePass in assessment.studentsUsingPasses)
            {
                var student = UserViewModel.Get(latePass.student.GetUserRecord(registrar));

                passes.Add(new()
                {
                    Student = student,
                    CourseName = assessment.section.displayName,
                    Timestamp = latePass.timeStamp
                });
            }

            return passes;
        }

        public static LatePassViewModel Get(AssessmentPassDetail model)
        {
            var registrar = ServiceHelper.GetService<RegistrarService>();
            var vm = new LatePassViewModel()
            {
                CourseName = registrar.CourseList.FirstOrDefault(course => course.courseCode == model.assessment.summary).displayName,
                Note = model.assessment.description,
                DateAndTime = model.assessment.start,
                Timestamp = model.timeStamp,
                Student = UserViewModel.Get(model.student),
                Model = OptionalStruct<AssessmentPassDetail>.Some(model)
            };

            return vm;
        }

        [RelayCommand]
        public void LoadAssessment() => LoadAssessmentRequested?.Invoke(this, Model.Reduce(AssessmentPassDetail.Empty));
    }
}
