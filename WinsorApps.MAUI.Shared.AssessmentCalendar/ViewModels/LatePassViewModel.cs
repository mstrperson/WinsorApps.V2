using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
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
        [ObservableProperty] UserViewModel student = UserViewModel.Default;
        public AssessmentPassDetail Model { get; private set; }

        public event EventHandler<AssessmentPassDetail>? LoadAssessmentRequested;

        private LatePassViewModel() { }

        public static LatePassViewModel Get(AssessmentPassDetail model)
        {
            var registrar = ServiceHelper.GetService<RegistrarService>();
            var vm = new LatePassViewModel()
            {
                CourseName = registrar.CourseList.First(course => course.courseCode == model.assessment.summary).displayName,
                Note = model.assessment.description,
                DateAndTime = model.assessment.start,
                Timestamp = model.timeStamp,
                Student = UserViewModel.Get(model.student),
                Model = model
            };

            return vm;
        }

        [RelayCommand]
        public void LoadAssessment() => LoadAssessmentRequested?.Invoke(this, Model);
    }
}
