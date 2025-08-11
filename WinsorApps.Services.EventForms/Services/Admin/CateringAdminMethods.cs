namespace WinsorApps.Services.EventForms.Services.Admin;

public partial class EventsAdminService
{
    private readonly EventFormsService _calendarService;

    public async Task UpdateCateringLaborCost(string eventId, double cost, ErrorAction onError) => 
        _ = await _api.SendAsync(HttpMethod.Put, $"api/events/{eventId}/catering/laborcost/{(int)(cost * 100)}", onError: onError);

    public async Task<byte[]> DownloadInvoice(string eventId, ErrorAction onError) => 
        await _api.DownloadFile($"api/events/{eventId}/catering/invoice", onError: onError);

    public async Task<byte[]> DownloadMonthlyReport(DateTime month, ErrorAction onError) => 
        await _api.DownloadFile($"api/events/catering/monthly-report?month={month:yyyy-MM-dd}", onError: onError);

    public async Task<byte[]> DownloadMonthlyInvoices(DateTime month, ErrorAction onError) => 
        await _api.DownloadFile($"api/events/catering/monthly-report/invoices?month={month:yyyy-MM-dd}", onError: onError);
}
