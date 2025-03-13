namespace WinsorApps.Services.EventForms.Models.Admin;


public record EventApprovalStatusRecord(string eventId, string status,
    DateTime timestamp, string managerId, string note = "");

public record CreateApprovalNote(string status, DateTime timestamp, string notes);