using CommunityToolkit.Mvvm.ComponentModel;
using WinsorApps.MAUI.Shared;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.Bookstore.ViewModels;
using WinsorApps.Services.Bookstore.Models;
using SectionRecord = WinsorApps.Services.Global.Models.SectionRecord;

namespace WinsorApps.MAUI.StudentBookstore.ViewModels
{
    public partial class StudentBookstoreViewModel(StudentBookstoreService bookService, RegistrarService registrarService) :
        ObservableObject,
        IErrorHandling,
        IBusyViewModel
    {
        private readonly StudentBookstoreService _bookService = bookService;
        private readonly RegistrarService _registrarService = registrarService;
        
        [ObservableProperty] private bool busy;
        [ObservableProperty] private string busyMessage = "Loading";
        [ObservableProperty] private bool ready;
        public event EventHandler<ErrorRecord>? OnError;

        [ObservableProperty] private ObservableCollection<SectionRequiredBooksViewModel> sectionRequiredBooks = [];

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
                    SectionRequiredBooks.Add(new(section, new("", [])));
                }
            }
            Busy = false;
            Ready = true;
        }
    }

    public partial class SectionRequiredBooksViewModel :
        ObservableObject
    {
        [ObservableProperty] private SectionViewModel section = SectionViewModel.Empty;
        [ObservableProperty] private ObservableCollection<OptionGroupViewModel> requiredBooks = [];

        private SectionRequiredBooksViewModel() { }

        public SectionRequiredBooksViewModel(SectionRecord section, StudentSectionBookRequirements books) 
        {
            this.section = SectionViewModel.Get(section);
            requiredBooks = [.. books.studentSections.Select(group => new OptionGroupViewModel(group))];
        }
    }

    public partial class OptionGroupViewModel :
        ObservableObject
    {
        private static readonly double heightPerBook = 100;
        [ObservableProperty] private string option = "";
        [ObservableProperty] private ObservableCollection<IsbnViewModel> books = [];
        [ObservableProperty] private double heightRequest;

        public OptionGroupViewModel(StudentSectionBookOptionGroup group) 
        {
            option = group.option;
            books = [.. group.isbns.Select(IsbnViewModel.Get)];
            HeightRequest = heightPerBook * Books.Count + 50;
        }
    }

}
