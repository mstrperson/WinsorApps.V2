using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.TeacherAssessmentCalendar.ViewModels;

namespace WinsorApps.MAUI.TeacherAssessmentCalendar.Pages;

public partial class AssessmentDetailPage : ContentPage
{
    public AssessmentDetailPage(AssessmentDetailsViewModel viewModel)
    {
        BindingContext = viewModel; 
        foreach (var studentEntry in viewModel.Students)
            studentEntry.Selected += async (sender, ent) =>
            {
                if (ent.PassAvailable)
                {
                    await viewModel.UseLatePassFor(ent.Student);
                }
                else if (ent.LatePassUsed)
                {
                    await viewModel.WithdrawPassFor(ent.Student);
                }
            };

        viewModel.LoadComplete += (sender, args) =>
        {
            foreach (var studentEntry in viewModel.Students)
                studentEntry.Selected += async (sender, ent) =>
                {
                    if (ent.PassAvailable)
                    {
                        await viewModel.UseLatePassFor(ent.Student);
                    }
                    else if (ent.LatePassUsed)
                    {
                        await viewModel.WithdrawPassFor(ent.Student);
                    }
                };
        };
        InitializeComponent();
    }
}