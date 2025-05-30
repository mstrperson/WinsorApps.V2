﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.Bookstore.ViewModels;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Bookstore.Services;
using WinsorApps.Services.Global.Models;


namespace WinsorApps.MAUI.TeacherBookOrders.ViewModels;

public partial class BookOrderTermCollection(TermRecord term) :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] ObservableCollection<ProtoSectionViewModel> sections = [];
    [ObservableProperty] ObservableCollection<SummerSectionViewModel> summer = [];
    [ObservableProperty] string termName = $"{term.name} {term.schoolYear}";

    public TermRecord Term { get; private set; } = term;

    public event EventHandler<TermRecord>? NewSectionRequested;

    [RelayCommand]
    public void RequestNewSection() => NewSectionRequested?.Invoke(this, Term);
}

public partial class BookOrderYearCollection :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    private readonly TeacherBookstoreService _service = ServiceHelper.GetService<TeacherBookstoreService>();

    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";
    [ObservableProperty] BookOrderTermCollection fall;
    [ObservableProperty] BookOrderTermCollection spring;
    [ObservableProperty] BookOrderTermCollection summer;
    [ObservableProperty] bool showSummer;
    [ObservableProperty] string schoolYear = "";
    [ObservableProperty] bool visible;
    [ObservableProperty] bool isCurrent;

    private readonly SchoolYear _schoolYear;

    public BookOrderYearCollection(SchoolYear schoolYear)
    {
        _schoolYear = schoolYear;
        SchoolYear = schoolYear.label;
        isCurrent = schoolYear.endDate.ToDateTime(default) > DateTime.Today;
        fall = new(schoolYear.terms.FirstOrDefault() ?? 
            new TermRecord("", schoolYear.label, "none", 
                schoolYear.startDate.ToDateTime(default), 
                schoolYear.endDate.ToDateTime(default)));
    }

    [RelayCommand]
    public void ToggleIsVisible()
    {
        Visible = !Visible;
    }

    [RelayCommand]
    public async Task LoadSections()
    {
        Busy = true;
        BusyMessage = $"Loading sections for {SchoolYear}";
        await _service.WaitForInit(OnError.DefaultBehavior(this));
        var sections = _service.MySections.Where(sec => sec.schoolYearId == _schoolYear.id).ToList();
        var allViewModels = sections.Select(ProtoSectionViewModel.Get).ToList();

        foreach (var section in allViewModels)
        {
            section.OnError += (sender, err) => OnError?.Invoke(sender, err);
            section.PropertyChanged += ((IBusyViewModel)this).BusyChangedCascade;
            await section.LoadBookOrders();
        }

        Fall.Sections = [.. allViewModels.Where(sec => sec.BookOrders.Any(grp => grp.Orders.Any(ord => ord.FallOrFullYear)))];
        Spring.Sections = [.. allViewModels.Where(sec => sec.BookOrders.Any(grp => grp.Orders.Any(ord => ord.SpringOnly)))];

        var ss = _service.SummerSections.GetOrAdd(SchoolYear, []);

        Summer.Summer = 
            [.. ss.Select(SummerSectionViewModel.Get)];
        Busy = false;
    }
}

public partial class BookOrderByYearPageViewModel :
    ObservableObject,
    IErrorHandling,
    IBusyViewModel
{
    public event EventHandler<ErrorRecord>? OnError;

    [ObservableProperty] bool busy;
    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] ObservableCollection<BookOrderYearCollection> years = [];


}
