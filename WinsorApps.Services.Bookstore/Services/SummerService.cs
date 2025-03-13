using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Bookstore.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WinsorApps.Services.Bookstore.Services;

public partial class TeacherBookstoreService
{
    public Dictionary<string, List<SummerSection>> SummerSections { get; private set; } = [];

    public async Task<Dictionary<string, List<SummerSection>>> GetSummerSections(ErrorAction onError)
    {
        var result = await _api.SendAsync<List<SummerSection>>(HttpMethod.Get, "api/book-orders/summer", onError: onError);

        if (result is not null)
        {
            SummerSections = result.SeparateByKeys(sec => sec.schoolYear);
        }

        return SummerSections;
    }

    public async Task<Optional<SummerSection>> CreateSummerSection(string courseId, ErrorAction onError)
    {
        var result = await _api.SendAsync<SummerSection?>(HttpMethod.Post, $"api/book-orders/summer/{courseId}", onError: onError);
        if (result is not null)
        {
            var section= result;
            SummerSections[section.schoolYear].Add(section);
            return Optional<SummerSection>.Some(section);
        }

        return Optional<SummerSection>.None();
    }

    public async Task DeleteSummerSection(SummerSection section, ErrorAction onError)
    {
        await _api.SendAsync(HttpMethod.Delete, $"api/book-orders/summer/{section.id}", onError: onError);

        SummerSections[section.schoolYear].Remove(section);
    }
    public async Task<Optional<SummerSection>> GetSummerSection(string sectionId, ErrorAction onError)
    {
        var result = await _api.SendAsync<SummerSection?>(HttpMethod.Get, $"api/book-orders/summer/{sectionId}", onError: onError);
        if (result is not null)
        {
            var section = result;
            SummerSections[section.schoolYear].ReplaceBy(section, sec => sec.id == sectionId);
            return Optional<SummerSection>.Some(section);
        }

        return Optional<SummerSection>.None();
    }

    public async Task<Optional<SummerSection>> AddOrUpdateSummerOrder(string sectionId, string isbn, int quantity, ErrorAction onError)
    {
        var result = await _api.SendAsync<SummerSection?>(HttpMethod.Post, $"api/book-orders/summer/{sectionId}/{isbn}?quantity={quantity}", onError: onError);
        if (result is not null)
        {
            var section = result;
            SummerSections[section.schoolYear].ReplaceBy(section, sec => sec.id == sectionId);
            return Optional<SummerSection>.Some(section);
        }

        return Optional<SummerSection>.None();
    }

    public async Task DeleteSummerBook(SummerSection section, string isbn, ErrorAction onError)
    {
        var result = await _api.SendAsync(HttpMethod.Delete, $"api/book-orders/summer/{section.id}/{isbn}", onError: onError);

        section = section with { books = [.. section.books.Where(ord => ord.isbn.isbn != isbn)] };

        SummerSections[section.schoolYear].ReplaceBy(section, sec => sec.id == section.id);
    }
}