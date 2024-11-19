global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using AsyncAwaitBestPractices;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;


namespace WinsorApps.Services.Global;

public class ServiceNotReadyException : Exception
{
    public ServiceNotReadyException(LocalLoggingService loggingService, string? message = null, Exception? innerException = null) : base(message, innerException)
    {
        loggingService.LogMessage(LocalLoggingService.LogLevel.Debug, $"A service was invoked before it is ready: {message ?? ""}");
    }
}
public static partial class RegexHelper
{

    [GeneratedRegex("^[a-z0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    public static partial Regex AlphaNumericNoSpaces();

    [GeneratedRegex("^\\$?[0-9]+(\\.[0-9]{2})?$", RegexOptions.Compiled)]
    public static partial Regex CurrencyValidator();

    [GeneratedRegex("^(978-)?[0-9]{10}$", RegexOptions.Compiled)]
    public static partial Regex IsbnValidator();

    [GeneratedRegex("^[[{]", RegexOptions.Compiled)]
    public static partial Regex ValidJsonStartChars();
}

/// <summary>
/// Helper construct for dealing with Months somewhat independent of DateTime or DateOnly.
/// Contains only static singletons for each of the months, and arithmetic operators 
/// +, -, ++, and -- on these months stay within the group.
/// </summary>
public class Month
{
    public static readonly Month January = new Month(1);
    public static readonly Month February = new Month(2);
    public static readonly Month March = new Month(3);
    public static readonly Month April = new Month(4);
    public static readonly Month May = new Month(5);
    public static readonly Month June = new Month(6);
    public static readonly Month July = new Month(7);
    public static readonly Month August = new Month(8);
    public static readonly Month September = new Month(9);
    public static readonly Month October = new Month(10);
    public static readonly Month November = new Month(11);
    public static readonly Month December = new Month(12);

    public static readonly ImmutableArray<Month> Months =
    [
        Month.January,
        Month.February,
        Month.March,
        Month.April,
        Month.May,
        Month.June,
        Month.July,
        Month.August,
        Month.September,
        Month.October,
        Month.November,
        Month.December
    ];

    private readonly int _month;
    private static readonly ImmutableArray<string> MonthNames =
    [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December"
    ];

    private Month(int m) { _month = m; }

    public static implicit operator int(Month m) => m._month;
    public static implicit operator Month(int m) => m switch
    {
        1 => January,
        2 => February,
        3 => March,
        4 => April,
        5 => May,
        6 => June,
        7 => July,
        8 => August,
        9 => September,
        10 => October,
        11 => November,
        12 => December,
        _ => throw new InvalidCastException($"Invalid Month {m}")
    };

    public static implicit operator Month(string m) => m.ToLowerInvariant().Trim() switch
    {
        "jan" or "january" => January,
        "feb" or "february" => February,
        "mar" or "march" => March,
        "apr" or "april" => April,
        "may" => May,
        "jun" or "june" => June,
        "jul" or "july" => July,
        "aug" or "august" => August,
        "sep" or "sept" or "september" => September,
        "oct" or "october" => October,
        "nov" or "november" => November,
        "dec" or "december" => December,
        _ => throw new InvalidCastException($"Invalid Month {m}")
    };

    public static Month operator --(Month month)
    {
        int m = ((int)month)-1;
        if (m == 0)
            m = 12;
        return (Month)m;
    }
    public static Month operator ++(Month month)
    {
        int m = ((int)month) + 1;
        if (m > 12)
            m = 1;
        return (Month)m;
    }

    public static Month operator +(Month month, int m) => (Month)(((((int)month) - 1 + m) % 12) + 1);
    public static Month operator -(Month month, int m) => (Month)(((((int)month) - 1 - m) % 12) + 1);

    public override string ToString() => $"{MonthNames[_month - 1]}";
}

public readonly record struct DateRangeWrapper(DateOnly start, DateOnly end)
{
    public static implicit operator DateRange(DateRangeWrapper wrapper) => new(wrapper.start, wrapper.end);
    public static implicit operator DateRangeWrapper(DateRange range) => new(range.start, range.end);

    public DateRangeWrapper(DateTime start, DateTime end) : this(DateOnly.FromDateTime(start), DateOnly.FromDateTime(end)) { }
}


/// <summary>
/// Enumerable range of dates.
/// </summary>
/// <param name="start"></param>
/// <param name="end"></param>
public record struct DateRange(DateOnly start, DateOnly end) : IEnumerable<DateOnly>
{
    public DateRange(DateTime start, DateTime end) : this(DateOnly.FromDateTime(start), DateOnly.FromDateTime(end)) { }

    public bool Contains(DateOnly date) => date >= start && date <= end;
    public bool Contains(DateTime date) => DateOnly.FromDateTime(date) >= start && DateOnly.FromDateTime(date) <= end;

    public static DateRange MonthOf(int month, int year = -1) => MonthOf(month, year);
    public static DateRange MonthOf(int month, DateTime starting)
    {
        if(month < starting.Month) 
            return MonthOf(month, starting.Year+1);

        return MonthOf(month, starting.Year);
    }

    public static DateRange MonthOf(Month month, DateTime starting) => 
        MonthOf(month, ((int)month) < starting.Month ? starting.Year+1 : starting.Year);
    public static DateRange MonthOf(int month, DateOnly starting)
    {
        if (month < starting.Month)
            return MonthOf(month, starting.Year + 1);

        return MonthOf(month, starting.Year);
    }

    public static DateRange MonthOf(Month month, DateOnly starting) =>
        MonthOf(month, ((int)month) < starting.Month ? starting.Year + 1 : starting.Year);

    public static DateRange MonthOf(Month month, int year = -1)
    {
        if (year < 1 || year > 9999)
            year = DateTime.Today.Year;
        var start = new DateOnly(year, (int)month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return new(start, end);
    }

    public IEnumerator<DateOnly> GetEnumerator() => new DateRangeEnumerator(ref this);

    IEnumerator IEnumerable.GetEnumerator() => new DateRangeEnumerator(ref this);

    public class DateRangeEnumerator : IEnumerator<DateOnly>
    {
        DateRange range;

        private DateOnly? current;
        public DateRangeEnumerator(ref DateRange range)
        {
            this.range = range;
        }

        public DateOnly Current => current.HasValue ? current.Value : default;

        object IEnumerator.Current => current.HasValue ? current.Value : default;

        public void Dispose()
        {
            
        }

        public bool MoveNext()
        {
            if (!current.HasValue) current = range.start;
            else current = current.Value.AddDays(1);
            return current.Value <= range.end;
        }

        public void Reset()
        {
            current = null;
        }
    }
}
public class DebugTimer : IDisposable
{
    Stopwatch sw;
    string message;
    private readonly LocalLoggingService _logging;

    public DebugTimer(string message, LocalLoggingService logging)
    {

        _logging = logging;
#if DEBUG
        Debug.WriteLine($"starting:  {message}");
#endif
        logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"starting:  {message}");
        sw = Stopwatch.StartNew();
        this.message = message;
    }

    public void Dispose()
    {
        sw.Stop();
#if DEBUG
        Debug.WriteLine($"{message} took {sw.ElapsedMilliseconds}ms.");
#endif
        _logging.LogMessage(LocalLoggingService.LogLevel.Debug, $"{message} took {sw.ElapsedMilliseconds}ms.");
    }
}
public class SixWeekPeriod
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate => StartDate.AddDays(6 * 7);

    public SixWeekPeriod Next => new SixWeekPeriod { StartDate = EndDate };

    public override string ToString() => string.Format("[{0} - {1}]", StartDate.ToShortDateString(), EndDate.ToShortDateString());

    /// <summary>
    /// Pass the format parameter to the two datetime objects, start and end of this period.
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    public string ToString(string format) => string.Format("[{0} - {1}]", StartDate.ToString(format), EndDate.ToString(format));

    /// <summary>
    /// Do the given Action once per date over the full six week period.
    /// </summary>
    /// <param name="action">delegate method call which takes a DateTime as its single parameter.</param>
    public void Iterate(Action<DateOnly> action)
    {
        for (DateOnly date = StartDate; date < EndDate; date = date.AddDays(1))
        {
            action(date);
        }
    }

    /// <summary>
    /// Do the given Action once per date over the full six week period.
    /// </summary>
    /// <param name="action">delegate method call which takes a DateTime as its single parameter.</param>
    public void ParallelIterate(Action<DateOnly> action, ParallelOptions? parallelOptions = null)
    {
        List<DateOnly> dateList = [];
        for (DateOnly date = StartDate; date < EndDate; date = date.AddDays(1))
        {
            dateList.Add(date);
        }

        if (parallelOptions is null)
            parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 16 };

        Parallel.ForEach(dateList, parallelOptions, action);
    }

    /// <summary>
    /// Holy shit that's one abstract method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="action"></param>
    /// <param name="iterationOperation"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public T ParallelIterate<T>(Func<DateOnly, T> action, Action<T, T> iterationOperation, T output, ParallelOptions? parallelOptions = null)
    {
        List<DateOnly> dateList = [];
        for (DateOnly date = StartDate; date < EndDate; date = date.AddDays(1))
        {
            dateList.Add(date);
        }

        if (parallelOptions is null)
            parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 16 };

        Parallel.ForEach(dateList, parallelOptions, date => iterationOperation(output, action(date)));

        return output;
    }

    /// <summary>
    /// Holy shit that's one abstract method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="action"></param>
    /// <param name="iterationOperation"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public T Iterate<T>(Func<DateOnly, T> action, Action<T, T> iterationOperation, T output)
    {
        for (DateOnly date = StartDate; date < EndDate; date = date.AddDays(1))
        {
            iterationOperation(output, action(date));
        }

        return output;
    }
}
public static partial class Extensions
{
    public static List<T> Merge<T>(this List<T> list, IEnumerable<T> other, Func<T, T, bool>? replacementCriteria = null)
    {
        foreach (var item in other)
        {
            if(replacementCriteria is not null)
            {
                var existing = list.FirstOrDefault(it => replacementCriteria(it, item));
                if (existing is not null && list.Contains(existing))
                    list.Remove(existing);
            }

            list.Add(item);
        }

        return list;
    }

    public static ImmutableArray<T> Merge<T>(this ImmutableArray<T> list, IEnumerable<T> other, Func<T, T, bool>? replacementCriteria = null)
    {
        foreach (var item in other)
        {
            if (replacementCriteria is not null)
            {
                var existing = list.FirstOrDefault(it => replacementCriteria(it, item));
                if (existing is not null && list.Contains(existing))
                    list = list.Remove(existing);
            }

            list = list.Add(item);
        }

        return list;
    }

    public static DateTime At(this DateOnly date, TimeOnly time) => new(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
    public static DateTime At(this DateOnly date, TimeSpan time) => new(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds);

    /// <summary>
    /// Separate all similar items in an Enumerable into lists based on a Key Selector function.
    /// All entities in the list that map to the same key will be collected into List and paired
    /// to that key in a Dictionary.
    /// </summary>
    /// <typeparam name="TKey">Type to index the lists by</typeparam>
    /// <typeparam name="TValue">DataType of the items in this List that will be separated.</typeparam>
    /// <param name="list">Collection of items to classify.</param>
    /// <param name="keySelector">items in the list that map to the same key via this function will be grouped together in the output.</param>
    /// <returns></returns>
    public static Dictionary<TKey, List<TValue>> SeparateByKeys<TKey, TValue>
        (this IEnumerable<TValue> list, Func<TValue, TKey> keySelector)
        where TKey : notnull
    {
        Dictionary<TKey, List<TValue>> output = new();

        foreach(TValue value in list)
        {
            var key = keySelector(value);
            if (!output.ContainsKey(key))
                output.Add(key, new());

            output[key].Add(value);
        }

        return output;
    }

    /// <summary>
    /// Get the first Monday before the given date,
    /// except Sunday goes forward 1 day.
    /// if the given date is a Monday, then this date is unchanged.
    /// 
    /// The returned DateTime object is only the Date component.
    /// So, even if a Monday is passed in, it will be stripped of
    /// the Time component of the object.
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime MondayOf(this DateTime date) => (date.DayOfWeek switch
    {
        DayOfWeek.Monday => date,
        DayOfWeek.Sunday => date.AddDays(1),
        _ => date.AddDays(1 - (int)date.DayOfWeek),
    }).Date;


    public static DateTime MonthOf(this DateTime date) => new(date.Year, date.Month, 1);

    /// <summary>
    /// Get the first Monday before the given date,
    /// except Sunday goes forward 1 day.
    /// if the given date is a Monday, then this date is unchanged.
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateOnly MondayOf(this DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => date,
        DayOfWeek.Sunday => date.AddDays(1),
        _ => date.AddDays(1 - (int)date.DayOfWeek),
    };

    /// <summary>
    /// Takes an enumerable list of strings, and combines them into a single string
    /// using the provided delimeter.
    /// You may select whether to allow repeated entries or empty entries.
    /// </summary>
    /// <param name="entries">The base enumerable list of strings.</param>
    /// <param name="delimeter">Separator between items.  
    ///     Default is `;` because this is mostly used to suplement CSV files,
    ///     but, you can pass stylistic things here as well such as `, ` to make a
    ///     nice clean list in print, or HTML tags for presenting web formatted data.</param>
    /// <param name="noRepeats">default: true - adds the Distinct() linq filter to the chain before aggregating</param>
    /// <param name="removeEmpty">default: true - removes any null or whitespace entries from the output before aggregating.</param>
    /// <returns></returns>
    public static string DelimeteredList(this IEnumerable<string> entries, string delimeter = ";", bool noRepeats = true, bool removeEmpty = true)
    {
        if (removeEmpty)
            entries = entries.Where(ent => !string.IsNullOrWhiteSpace(ent));

        if (!entries.Any())
            return "";

        if (noRepeats)
            entries = entries.Distinct();
        return entries.Aggregate((a, b) => $"{a}{delimeter}{b}");
    }

    public static IEnumerable<string> DelimeteredList(this string list, char delimeter = ';', bool noRepeats = true,
        bool removeEmpty = true)
    {
        var output = list.Split(delimeter).Select(str => str.Trim());
        if (noRepeats)
            output = output.Distinct();
        if (removeEmpty)
            output = output.Where(str => !string.IsNullOrWhiteSpace(str));
        return output;
    }

    /// <summary>
    /// Canned logging behavior to be implemented wherever API calls might go wrong.
    /// </summary>
    /// <param name="onError"></param>
    /// <param name="logging"></param>
    /// <returns></returns>
    public static Action<HttpResponseMessage> DefaultErrorHandler(this ErrorAction onError, LocalLoggingService logging)
    {
        return response =>
        {
            var json = response.Content.ReadAsStringAsync().Result;
            try
            {
                var error = JsonSerializer.Deserialize<ErrorRecord>(json)!;
                onError(error);
            }
            catch (Exception e)
            {
                logging.LogMessage(LocalLoggingService.LogLevel.Error, 
                    $"Error Decoding Response to {response.RequestMessage?.RequestUri?.ToString() ?? "unknown request"}", 
                    $"Status Code: {response.StatusCode}",
                    e.Message, e.StackTrace ?? "no Stack trace");
                onError(new ErrorRecord("Error Decoding Response", string.IsNullOrEmpty(json) ?
                    $"Error Decoding Response to {response.RequestMessage?.RequestUri?.ToString() ?? "unknown request"}" +
                    $"\tStatus Code: {response.StatusCode}" :
                    json));
            }
        };
    }

    /// <summary>
    /// Short-hand overload for .GetAwaiter().OnCompleted(...)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <param name="continuation"></param>
    public static void WhenCompleted<T>(this Task<T> task, Action continuation, Action? taskCanceledAction = null)
    {
        
        task.GetAwaiter().OnCompleted(() =>
        {
            if (!task.IsCompletedSuccessfully)
            {
                taskCanceledAction?.Invoke();
                return;
            }
            continuation?.Invoke();
        });
    }

    /// <summary>
    /// Short-hand overload for .GetAwaiter().OnCompleted(...)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <param name="continuation"></param>
    public static void WhenCompleted(this Task task, Action continuation) => task.GetAwaiter().OnCompleted(continuation);

    /// <summary>
    /// Logs the full exception chain into the Error log.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="loggingService"></param>
    public static void LogException(this Exception? e, LocalLoggingService loggingService)
    {
        for (; e is not null; e = e.InnerException)
        {
            loggingService.LogMessage(LocalLoggingService.LogLevel.Error,
                e.Message, e.StackTrace ?? "");
        }
    }

    /// <summary>
    /// Quick test to see if the Task is still running.  Haven't really used this, but it might be nice?
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public static bool IsWaiting(this Task task) => (int)task.Status < 3;

    /// <summary>
    /// Ok, this method is lazy, and pushing the bounds of what's appropriate...
    /// basically just extracts a double from text.  You can choose whether it 
    /// throws an exception or just returns Zero as the default behavior for 
    /// invalid input.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="throwIfInvalid"></param>
    /// <returns></returns>
    /// <exception cref="InvalidCastException"></exception>
    public static double ConvertToCurrency(this string input, bool throwIfInvalid = false)
    {
        if (input is null)
            input = string.Empty;
        input = input.Trim();
        if (input.StartsWith("$"))
            input = input.Substring(1);

        input = input.Trim();

        if (double.TryParse(input, out double result))
            return result;

        if (throwIfInvalid)
            throw new InvalidCastException($"`${input}` is not a valid currency value.");

        return 0;
    }

    /// <summary>
    /// Replace an item in a list... How is this not a built in thing?
    /// Returns the list itself to maintain fluency, but it is not required.
    /// this mutates the provided list reference.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="oldItem"></param>
    /// <param name="newItem"></param>
    /// <returns></returns>
    public static List<T> Replace<T>(this List<T> list, T oldItem, T newItem)
    {
        var i = list.IndexOf(oldItem);
        if (i != -1)
            list.Remove(oldItem);
        else i = 0;

        list.Insert(i, newItem);
        return list;
    }

    /// <summary>
    /// Uses SafeFireAndForget to start a whole bunch of task
    /// </summary>
    /// <param name="tasks"></param>
    /// <param name="errorHandler"></param>
    public static void StartAll(this IEnumerable<Task> tasks, Action<Exception>? errorHandler = null)
    {
        foreach (var task in tasks)
            task.SafeFireAndForget(errorHandler);
    }

    public static UserRecord GetUserRecord(this StudentRecordShort student, RegistrarService registrar) => registrar.StudentList.FirstOrDefault(u => u.id == student.id);
}
