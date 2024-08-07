﻿global using ErrorAction = System.Action<WinsorApps.Services.Global.Models.ErrorRecord>;
using WinsorApps.Services.EventForms.Services;

namespace WinsorApps.MAUI.Shared.EventForms;

public static partial class Extensions
{
    public static MauiAppBuilder AddEventFormsServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<BudgetCodeService>();
        builder.Services.AddSingleton<ReadonlyCalendarService>();
        builder.Services.AddSingleton<EventFormsService>();
        builder.Services.AddSingleton<ContactService>();
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<CateringMenuService>();
        builder.Services.AddSingleton<TheaterService>();

        return builder;
    }
}
