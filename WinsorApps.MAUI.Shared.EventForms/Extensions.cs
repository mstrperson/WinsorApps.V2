﻿global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.EventForms.Services;

namespace WinsorApps.MAUI.Shared.EventForms;

public static partial class Extensions
{
    public static MauiAppBuilder AddEventFormsServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<BudgetCodeService>();
        builder.Services.AddSingleton<ReadonlyCalendarService>();

        return builder;
    }
}
