namespace WinsorApps.Services.EventForms.Models.Admin;


public readonly record struct EventApprovalStatusRecord(string eventId, string status,
    DateTime timestamp, string managerId, string note = "");

public readonly record struct CreateApprovalNote(string status, DateTime timestamp, string notes);