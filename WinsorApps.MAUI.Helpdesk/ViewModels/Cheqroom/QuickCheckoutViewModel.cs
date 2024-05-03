using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.MAUI.Shared;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Services;

namespace WinsorApps.MAUI.Helpdesk.ViewModels.Cheqroom
{
    public partial class QuickCheckoutViewModel : ObservableObject, IErrorHandling
    {
        private readonly CheqroomService _cheqroom;

        public QuickCheckoutViewModel(CheqroomService cheqroom)
        {
            _cheqroom = cheqroom;
        }

        [ObservableProperty] public string assetTag = "";
        [ObservableProperty] public UserSearchViewModel userSearch = new();
        [ObservableProperty] public CheckoutResultViewModel result = IEmptyViewModel<CheckoutResultViewModel>.Empty;
        [ObservableProperty] public bool displayResult;
        [ObservableProperty] public TimeSpan resultTimeout = TimeSpan.FromSeconds(15);
        public event EventHandler<ErrorRecord>? OnError;
        public event EventHandler<CheckoutResultViewModel>? OnCheckoutSuccessful;

        [RelayCommand]
        public async Task Checkout()
        {
            if (string.IsNullOrEmpty(AssetTag))
            {
                OnError?.Invoke(this, new("Missing Information", "You must enter an Asset Tag."));
                return;
            }
            if (!UserSearch.IsSelected)
            {
                OnError?.Invoke(this, new("Missing Information", $"Please select a person to check out {AssetTag} to."));
                return;
            }

            var result = await _cheqroom.QuickCheckOutItem(AssetTag, UserSearch.Selected.Id, err => OnError?.Invoke(this, err));

            if (!string.IsNullOrEmpty(result._id))
            {
                Result = new(result);
                OnCheckoutSuccessful?.Invoke(this, Result);
                DisplayResult = true;
                TimeOutFireAndForget();
            }
        }

        private void TimeOutFireAndForget()
        {
            var task = Task.Delay(ResultTimeout);
            task.WhenCompleted(() =>
            {
                if (DisplayResult)
                {
                    Clear();
                }
            });
            task.SafeFireAndForget(e => e.LogException(ServiceHelper.GetService<LocalLoggingService>()));
        }

        [RelayCommand]
        public void Clear()
        {
            AssetTag = "";
            UserSearch.ClearSelection();
            Result = IEmptyViewModel<CheckoutResultViewModel>.Empty;
            DisplayResult = false;
        }
    }
}
