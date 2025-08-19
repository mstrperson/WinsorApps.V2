using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.AssessmentCalendar.Models;
using WinsorApps.Services.AssessmentCalendar.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

public partial class NotesPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly AssessmentCalendarRestrictedService _service = ServiceHelper.GetService<AssessmentCalendarRestrictedService>();

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] ObservableCollection<NoteViewModel> notes = [];
    [ObservableProperty] NoteViewModel selectedNote = new();
    [ObservableProperty] bool showEditor;


    private void LinkEvents(NoteViewModel note)
    {
        note.Selected += (_, _) =>
        {
            SelectedNote = note;
            ShowEditor = true;
        };
        note.OnError += (s, e) => OnError?.Invoke(this, e);
        note.Saved += (_, saved) =>
        {
            if (Notes.All(n => n.Id != saved.Id))
                Notes.Add(saved);
            ShowEditor = false;
        };

        note.Deleted += (_, deleted) =>
        {
            Notes.Remove(deleted);
            ShowEditor = false;
        };

        note.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
    }

    public async Task Initialize()
    {
        Busy = true;
        BusyMessage = "Loading notes...";
        SelectedNote.Selected += (_, note) => SelectedNote = note;
        try
        {
            var notesList = await _service.GetNotes(OnError.DefaultBehavior(this));
            Notes = [..notesList.Select(NoteViewModel.Get)];
            foreach (var note in Notes)
                LinkEvents(note);
        }
        catch (Exception ex)
        {
            ex.LogException();
            OnError?.Invoke(this, new ErrorRecord(ex.Message, "InitializationError"));
        }
        finally
        {
            Busy = false;
            BusyMessage = "";
        }
    }

    [RelayCommand]
    public void New()
    {
        ShowEditor = true;
        var newNote = new NoteViewModel();
        LinkEvents(newNote);
        SelectedNote = newNote;
    }
}

public partial class NoteViewModel :
    ObservableObject,
    ISelectable<NoteViewModel>,
    IModelCarrier<NoteViewModel, AssessmentCalendarDisplayRecord>,
    IErrorHandling,
    IBusyViewModel
{
    private readonly AssessmentCalendarRestrictedService _service = ServiceHelper.GetService<AssessmentCalendarRestrictedService>();

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<NoteViewModel>? Selected;
    public event EventHandler<NoteViewModel>? Saved;
    public event EventHandler<NoteViewModel>? Deleted;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] string id = "";
    [ObservableProperty] bool isSelected;
    [ObservableProperty] string note = "";
    [ObservableProperty] ObservableCollection<SelectableLabelViewModel> affectedClasses =
        [
            new("Class V"),
            new("Class VI"),
            new("Class VII"),
            new("Class VIII")
        ];
    [ObservableProperty] DateTime date = DateTime.Today;


    [ObservableProperty] string classList = "";

    public NoteViewModel()
    {
        foreach(var label in AffectedClasses)
            label.Selected += (_, _) => ClassList = string.Join(", ", AffectedClasses.Where(lbl => lbl.IsSelected).Select(lbl => lbl.Label));
    }

    public Optional<AssessmentCalendarDisplayRecord> Model { get; private set; } = Optional<AssessmentCalendarDisplayRecord>.None();

    [RelayCommand]
    public void Select()
    {
        IsSelected = true;
        Selected?.Invoke(this, this);
    }

    [RelayCommand]
    public async Task Delete()
    {
        if (string.IsNullOrEmpty(Id))
            return;

        Busy = true;
        BusyMessage = "Deleting Note";

        await _service.DeleteNote(Id, OnError.DefaultBehavior(this));
        Deleted?.Invoke(this, this);
        Busy = false;
    }

    [RelayCommand]
    public async Task Save()
    {
        if(string.IsNullOrEmpty(Note))
        {
            OnError?.Invoke(this, new ErrorRecord("Validation Error", "Note cannot be empty."));
            return;
        }

        if(AffectedClasses.All(lbl => !lbl.IsSelected))
        {
            OnError?.Invoke(this, new ErrorRecord("Validation Error", "At least one class must be selected."));
            return;
        }

        if(Date < DateTime.Today)
        {
            OnError?.Invoke(this, new ErrorRecord("Validation Error", "Date cannot be in the past."));
            return;
        }

        Busy = true;
        BusyMessage = "Saving Note";

        var dto = new CreateAssessmentCalendarNote(Note,
            [.. AffectedClasses.Where(lbl => lbl.IsSelected).Select(lbl => lbl.Label)],
            DateOnly.FromDateTime(Date));

        var result = string.IsNullOrEmpty(Id) 
            ? await _service.CreateNote(dto, OnError.DefaultBehavior(this))
            : await _service.UpdateNote(Id, dto, OnError.DefaultBehavior(this));

        if(result is not null)
            Id = result.id;

        ClassList = AffectedClasses.Where(lbl => lbl.IsSelected).Select(lbl => lbl.Label).DelimeteredList(", ");

        Saved?.Invoke(this, this);
        Busy = false;
    }

    public static NoteViewModel Get(AssessmentCalendarDisplayRecord model)
    {
        var vm = new NoteViewModel
        {
            Id = model.id,
            Model = Optional<AssessmentCalendarDisplayRecord>.Some(model),
            Note = model.text,
            ClassList = model.affectedClasses.DelimeteredList(", "),
            Date = model.startDateTime,
        };

        foreach(var label in vm.AffectedClasses)
            label.IsSelected = model.affectedClasses.Any(lbl => lbl.Equals(label.Label, StringComparison.InvariantCultureIgnoreCase));

        return vm;
    }
}