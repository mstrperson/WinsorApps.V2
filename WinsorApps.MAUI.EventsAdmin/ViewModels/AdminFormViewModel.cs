﻿using CommunityToolkit.Mvvm.ComponentModel;
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
    IBusyViewModel,
    ISelectable<AdminFormViewModel>
{
    private readonly EventsAdminService _admin = ServiceHelper.GetService<EventsAdminService>();

    [ObservableProperty] EventFormViewModel form;
    [ObservableProperty] ObservableCollection<ApprovalRecordViewModel> approvalHistory = [];
    [ObservableProperty] ApprovalNoteEditorViewModel noteEditor = new();
    [ObservableProperty] bool showNoteEditor;
    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] bool isAdmin;
    [ObservableProperty] bool isRegistrar;

    [ObservableProperty] bool isSelected;
    [ObservableProperty] string roomList;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler? StatusChanged;
    public event EventHandler<AdminFormViewModel>? Selected;

    public static implicit operator AdminFormViewModel(EventFormViewModel form) => new(form);
    public static implicit operator AdminFormViewModel(EventFormBase form) => new(EventFormViewModel.Get(form));

    public AdminFormViewModel(EventFormViewModel form)
    {
        this.form = form;
        Form.ApproveRequested += async (_, _) => await Approve();
        Form.DeleteRequested += async (_, _) => await Reject();
        Form.ApproveRoomRequested += async (_, _) => await ApproveRoomUse();

        form.Selected += (_, _) => Selected?.Invoke(this, this);
        var registrar = ServiceHelper.GetService<RegistrarService>();
        isAdmin = registrar.MyRoles.Intersect(["System Admin", "Winsor - Events Admin"]).Any();
        isRegistrar = registrar.MyRoles.Intersect(["System Admin", "Registrar"]).Any();
        RoomList = form.SelectedLocations.Any() ? form.SelectedLocations.Select(loc => loc.Label).DelimeteredList() : "None!";
    }

    [RelayCommand]
    public async Task LoadHistory()
    {
        Busy = true;
        BusyMessage = $"Getting History for {Form.Summary}";
        var result = await _admin.GetHistory(Form.Id, OnError.DefaultBehavior(this));
        ApprovalHistory = [.. result.Select(ApprovalRecordViewModel.Get)];
        Busy = false;
    }

    [RelayCommand]
    public void ToggleShowNoteEditor()
    {
        ShowNoteEditor = !ShowNoteEditor;
        if(ShowNoteEditor)
        {
            NoteEditor.Note = "";
            NoteEditor.Status = Form.StatusSelection.Selected.Label;
        }
    }

    [RelayCommand]
    public async Task Approve()
    {
        if (!IsAdmin) return;
        Busy = true;
        BusyMessage = $"Approving {Form.Summary}";
        var result = await _admin.ApproveEvent(Form.Id, OnError.DefaultBehavior(this));
        var form = result.Reduce(EventFormBase.Empty);
        if (form.id == Form.Id) 
        {
            Form.StatusSelection.Select(form.status);
            await LoadHistory();
        }
        Busy = false;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task Reject()
    {
        if (!IsAdmin) return;
        Busy = true;
        BusyMessage = $"Declining {Form.Summary}";
        var result = await _admin.DeclineEvent(Form.Id, OnError.DefaultBehavior(this));
        var form = result.Reduce(EventFormBase.Empty);
        if (form.id == Form.Id)
        {
            Form.StatusSelection.Select(form.status);
            await LoadHistory();
        }
        Busy = false;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task ApproveRoomUse()
    {
        if (!IsRegistrar) return;
        Busy = true;
        BusyMessage = $"Approving Room for {Form.Summary}";
        var result = await _admin.ApproveRoomUse(Form.Id, OnError.DefaultBehavior(this));
        
        if (result.HasValue)
        {
            Form.StatusSelection.Select(result.Value.status);
            await LoadHistory();
        }
        Busy = false;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task RevokeRoomUse()
    {
        if (!IsRegistrar) return;
        Busy = true;
        BusyMessage = $"Revoking Room for {Form.Summary}";
        var result = await _admin.RevokeRoomUse(Form.Id, NoteEditor.Note, OnError.DefaultBehavior(this));

        if (result.HasValue)
        {
            Form.StatusSelection.Select(result.Value.status);
            await LoadHistory();
        }

        Busy = false;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task SubmitNote()
    {
        Busy = true;
        BusyMessage = $"Sending Note to {Form.Creator.DisplayName} about {Form.Summary}";
        await _admin.SendNote(Form.Id, NoteEditor, OnError.DefaultBehavior(this));
        await LoadHistory();
        Busy = false;
    }

    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
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
