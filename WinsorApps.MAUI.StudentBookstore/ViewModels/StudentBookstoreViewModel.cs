using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.Bookstore.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using Microsoft.IdentityModel.Tokens;
using SectionRecord = WinsorApps.Services.Global.Models.SectionRecord;
using WinsorApps.Services.Global;

namespace WinsorApps.MAUI.StudentBookstore.ViewModels
{
    public partial class StudentBookstoreViewModel :
        ObservableObject,
        IErrorHandling,
        IBusyViewModel
    {
        private readonly StudentBookstoreService _bookService = ServiceHelper.GetService<StudentBookstoreService>();
        private readonly RegistrarService _registrarService = ServiceHelper.GetService<RegistrarService>();
        
        [ObservableProperty] bool busy;
        [ObservableProperty] string busyMessage = "Loading";
        [ObservableProperty] bool ready;
        public event EventHandler<ErrorRecord>? OnError;

        [ObservableProperty] ObservableCollection<SectionRequiredBooksViewModel> sectionRequiredBooks = [];

        public StudentBookstoreViewModel()
        {
            
        }

        public async Task Initialize(ErrorAction onError)
        {
            Busy = true;
            var regtask = _registrarService.WaitForInit(onError);
            var bookTask = _bookService.WaitForInit(onError);
            await Task.WhenAll(regtask, bookTask);
           
            var myScehdule = _registrarService.MyAcademicSchedule;
            var requirements = await _bookService.GetSemesterBookList(DateTime.Today.Month < 11 && DateTime.Today.Month > 2, OnError.DefaultBehavior(this));

            foreach (var section in myScehdule)
            {
                if (requirements.schedule.Any(s => s.sectionId == section.sectionId))
                {
                    var requiredBooks = requirements.schedule.First(s => s.sectionId == section.sectionId);
                    SectionRequiredBooks.Add(new(section, requiredBooks));
                }
                else
                {
                    SectionRequiredBooks.Add(new(section, new()));
                }
            }
            Busy = false;
            Ready = true;
        }
    }

    public partial class SectionRequiredBooksViewModel :
        ObservableObject
    {
        [ObservableProperty] SectionViewModel section = SectionViewModel.Default;
        [ObservableProperty] ObservableCollection<OptionGroupViewModel> requiredBooks = [];

        public SectionRequiredBooksViewModel(SectionRecord section, StudentSectionBookRequirements books) 
        {
            this.section = SectionViewModel.Get(section);
            requiredBooks = [.. books.studentSections.Select(group => new OptionGroupViewModel(group))];
        }
    }

    public partial class OptionGroupViewModel :
        ObservableObject
    {
        [ObservableProperty] string option = "";
        [ObservableProperty] ObservableCollection<IsbnViewModel> books = [];

        public OptionGroupViewModel(StudentSectionBookOptionGroup group) 
        {
            option = group.option;
            books = [.. group.isbns.Select(IsbnViewModel.Get)];
        }
    }

}
