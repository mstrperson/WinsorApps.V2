using WinsorApps.Services.Bookstore.Models;

namespace WinsorApps.Services.Bookstore.Services;

public partial class BookstoreManagerService
{
    public List<StudentSectionBookOrder> StudentOrders { get; private set; } = [];

    public Dictionary<string, StudentSectionBookRequirements> StudentBookRequirementsBySection = [];

    public async Task DownloadStudentBookOrders(ErrorAction onError, Action<byte[]> completion)
    {
        var result = await _api.DownloadFile("api/book-orders/manager/student-orders", onError: onError);
        if (result.Length > 0)
        {
            completion(result);
        }
    }
    public async Task<StudentSectionBookOrder?> EditStudentBookOrder(string studentId, string sectionId,
        IEnumerable<string> selectedIsbns, ErrorAction onError)
    {
        var result = await _api.SendAsync<IEnumerable<string>, StudentSectionBookOrder?>(
            HttpMethod.Post,
            $"api/book-orders/students/{studentId}/orders/{sectionId}",
            selectedIsbns,
            onError: onError);

        if (result is null)
            return result;

        StudentOrders = [.. StudentOrders.Except(StudentOrders.Where(ord => ord.sectionId == sectionId && ord.student.id == studentId)), result];

        return result;
    }

    public Dictionary<string, List<StudentSectionBookRequirements>> RequiredBooksByStudent { get; private set; } = [];

    public async Task<List<StudentSectionBookRequirements>> GetStudentBookRequirements(string studentId, ErrorAction onError)
    {
        if(RequiredBooksByStudent.TryGetValue(studentId, out var value)) 
            return value ?? [];

        var result = await _api.SendAsync<StudentSemesterBookList?>(HttpMethod.Get,
            $"api/book-orders/manager/students/{studentId}/book-list", onError: onError);

        if(result is not null)
        {
            RequiredBooksByStudent.Add(studentId, result.schedule);
        }

        return result?.schedule ?? [];
    }

    public async Task<List<StudentSectionBookOrder>> GetStudentOrders(ErrorAction onError)
    {
        var result = await _api.SendAsync<List<StudentSectionBookOrder>?>(HttpMethod.Get,
            "api/book-orders/manager/students/orders", onError: onError);

        StudentOrders = result ?? [];

        return StudentOrders;
    }

    public async Task<List<StudentSectionBookOrder>> MarkCompletedOrders(string studentId, string[] selectedIsbns, ErrorAction onError)
    {
        var result = await _api.SendAsync<string[], List<StudentSectionBookOrder>?>(HttpMethod.Post,
            $"api/book-orders/manager/students/{studentId}/mark-completed", selectedIsbns, onError: onError);
        if(result is not null)
        {
            StudentOrders = [.. StudentOrders.Except(StudentOrders.Where(order => order.student.id == studentId)), .. result];
        }

        return result ?? [];
    }
}
