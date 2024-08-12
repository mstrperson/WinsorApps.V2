using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.EventForms.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Models.Admin;
using WinsorApps.Services.EventForms.Services.Admin;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.EventsAdmin.ViewModels;

public partial class AdminFormViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly EventsAdminService _admin = ServiceHelper.GetService<EventsAdminService>();

    [ObservableProperty] EventFormViewModel form;
    [ObservableProperty] ObservableCollection<ApprovalRecordViewModel> approvalHistory = [];
    [ObservableProperty] ApprovalNoteEditorViewModel noteEditor = new();
    [ObservableProperty] bool showNoteEditor;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    public event EventHandler<ErrorRecord>? OnError;

    public AdminFormViewModel(EventFormViewModel form)
    {
        this.form = form;
    }

    [RelayCommand]
    public async Task LoadHistory()
    {

    }
}

public partial class ApprovalNoteEditorViewModel :
    ObservableObject
{
    [ObservableProperty] ObservableCollection<string> statusChoices = 
    [
        ApprovalStatusLabel.Pending, 
        ApprovalStatusLabel.Approved, 
        ApprovalStatusLabel.Declined, 
        ApprovalStatusLabel.RoomNotCleared,
        ApprovalStatusLabel.Withdrawn
    ];

    [ObservableProperty] string status = ApprovalStatusLabel.Pending;
    [ObservableProperty] string note = "";

    public static implicit operator CreateApprovalNote(ApprovalNoteEditorViewModel editor) => new(editor.Status, DateTime.Now, editor.Note);
}

public partial class ApprovalRecordViewModel :
    ObservableObject,
    IModelCarrier<ApprovalRecordViewModel, EventApprovalStatusRecord>
{
    [ObservableProperty] string status = "";
    [ObservableProperty] UserViewModel manager = UserViewModel.Empty;
    [ObservableProperty] string note = "";
    [ObservableProperty] DateTime timeStamp;

    public OptionalStruct<EventApprovalStatusRecord> Model { get; private set; }

    public static ApprovalRecordViewModel Get(EventApprovalStatusRecord model)
    {
        var registrar = ServiceHelper.GetService<RegistrarService>();
        var mgr = registrar.AllUsers.FirstStructOrDefault(user => user.id == model.managerId, UserRecord.Empty);
        return new()
        {
            Model = OptionalStruct<EventApprovalStatusRecord>.Some(model),
            Status = model.status,
            Manager = UserViewModel.Get(mgr),
            Note = model.note,
            TimeStamp = model.timestamp
        };
    }
}
