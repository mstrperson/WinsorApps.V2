using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Bookstore.Models;

namespace WinsorApps.Services.Bookstore.Services;

public partial class BookstoreManagerService
{
    public ImmutableArray<StudentSectionBookOrder> StudentOrders { get; private set; } = [];

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

        if (!result.HasValue)
            return result;

        StudentOrders = [.. StudentOrders.Except(StudentOrders.Where(ord => ord.sectionId == sectionId && ord.student.id == studentId)), result.Value];

        return result;
    }

    public Dictionary<string, ImmutableArray<StudentSectionBookRequirements>> RequiredBooksByStudent { get; private set; } = [];

    public async Task<ImmutableArray<StudentSectionBookRequirements>> GetStudentBookRequirements(string studentId, ErrorAction onError)
    {
        if(RequiredBooksByStudent.TryGetValue(studentId, out ImmutableArray<StudentSectionBookRequirements> value)) 
            return value;

        var result = await _api.SendAsync<StudentSemesterBookList?>(HttpMethod.Get,
            $"api/book-orders/manager/students/{studentId}/book-list");

        if(result.HasValue)
        {
            RequiredBooksByStudent.Add(studentId, result.Value.schedule);
        }

        return result?.schedule ?? [];
    }

    public async Task<ImmutableArray<StudentSectionBookOrder>> GetStudentOrders(ErrorAction onError)
    {
        var result = await _api.SendAsync<ImmutableArray<StudentSectionBookOrder>?>(HttpMethod.Get,
            "api/book-orders/manager/students/orders", onError: onError);

        StudentOrders = result ?? [];

        return StudentOrders;
    }

    public async Task<ImmutableArray<StudentSectionBookOrder>> MarkCompletedOrders(string studentId, string[] selectedIsbns, ErrorAction onError)
    {
        var result = await _api.SendAsync<string[], ImmutableArray<StudentSectionBookOrder>?>(HttpMethod.Post,
            $"api/book-orders/manager/students/{studentId}/mark-completed", selectedIsbns, onError: onError);
        if(result.HasValue)
        {
            StudentOrders = [.. StudentOrders.Except(StudentOrders.Where(order => order.student.id == studentId)), .. result.Value];
        }

        return result ?? [];
    }
}
