﻿using Android.App;
using Android.Runtime;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar
{
    [Application]
    public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
