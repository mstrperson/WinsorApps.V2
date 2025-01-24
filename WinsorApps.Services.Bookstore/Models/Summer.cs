using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Bookstore.Models;
public readonly record struct SummerSection(string id, AdvisorRecord teacher, CourseRecord course, string schoolYear, DateTime submitted, ImmutableArray<SummerBookOrderListItem> books);

public readonly record struct SummerBookOrderListItem(ISBNDetail isbn, int quantity);