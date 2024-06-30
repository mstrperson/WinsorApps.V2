﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WinsorApps.MAUI.Shared.ViewModels;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.EventForms.Services;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using Contact = WinsorApps.Services.EventForms.Models.Contact;

namespace WinsorApps.MAUI.Shared.EventForms.ViewModels;

public partial class ContactViewModel :
    ObservableObject,
    IDefaultValueViewModel<ContactViewModel>,
    ICachedViewModel<ContactViewModel, Contact, ContactService>,
    IErrorHandling,
    ISelectable<ContactViewModel>,
    IModelCarrier<ContactViewModel, Contact>
    
{
    private readonly ContactService contactService = ServiceHelper.GetService<ContactService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string firstName = "";
    [ObservableProperty] string fullName = "";
    [ObservableProperty] string lastName = "";
    [ObservableProperty] string email = "";
    [ObservableProperty] string phone = "";
    [ObservableProperty] UserViewModel associatedUser = UserViewModel.Default;
    [ObservableProperty] bool isPublic;
    [ObservableProperty] bool isSelected;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<ContactViewModel>? Selected;
    public event EventHandler<ContactViewModel>? Created;

    [RelayCommand]
    public async Task Create()
    {
        var newContact = new NewContact(FirstName, LastName, Email, Phone, IsPublic);
        var result = await contactService.CreateNewContact(newContact, OnError.DefaultBehavior(this));
        if (!string.IsNullOrEmpty(result.id))
        {
            this.Id = result.id;
            Created?.Invoke(this, Get(result));
        }
    }
    
    public static ConcurrentBag<ContactViewModel> ViewModelCache { get; private set; } = [];

    public static ContactViewModel Default => new();

    public Contact Model { get; private set; }

    public static ContactViewModel Get(Contact model)
    {
        var vm = ViewModelCache.FirstOrDefault(con => con.Id == model.id);
        if (vm is not null) return vm.Clone();

        vm = new ContactViewModel
        {
            Id = model.id,
            FirstName = model.firstName,
            LastName = model.lastName,
            FullName = model.FullName,
            Email = model.email,
            Phone = model.phone,
            IsPublic = model.isPublic
        };

        if (!string.IsNullOrEmpty(model.associatedUserId))
        {
            var user = UserViewModel.ViewModelCache.FirstOrDefault(u => u.Id == model.associatedUserId);
            if (user is not null)
                vm.AssociatedUser = user.Clone();
        }

        ViewModelCache.Add(vm);
        return vm.Clone();
    }
    
    

    public static List<ContactViewModel> GetClonedViewModels(IEnumerable<Contact> models)
    {
        List<ContactViewModel> result = [];

        foreach (var model in models)
            result.Add(Get(model));

        return result;
    }

    public static async Task Initialize(ContactService service, ErrorAction onError)
    {
        service.OnCacheRefreshed += (_, _) =>
        {
            ViewModelCache = [];
            _ = GetClonedViewModels(service.MyContacts);
        };
        await service.WaitForInit(onError);
        _ = GetClonedViewModels(service.MyContacts);
    }

    public ContactViewModel Clone() => (ContactViewModel)MemberwiseClone();
    
    [RelayCommand]
    public void Select()
    {
        IsSelected = !IsSelected;
        Selected?.Invoke(this, this);
    }
}

public partial class ContactSearchViewModel :
    ObservableObject,
    ICachedSearchViewModel<ContactViewModel>,
    IErrorHandling
{
    private readonly ContactService _contactService = ServiceHelper.GetService<ContactService>();
    private readonly LocalLoggingService _logging = ServiceHelper.GetService<LocalLoggingService>();

    [ObservableProperty] ObservableCollection<ContactViewModel> available = [];
    [ObservableProperty] ObservableCollection<ContactViewModel> allSelected = [];
    [ObservableProperty] ObservableCollection<ContactViewModel> options = [];
    [ObservableProperty] ContactViewModel selected = ContactViewModel.Default;
    [ObservableProperty] SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty] string searchText = "";
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool showOptions;
    [ObservableProperty] bool showCreate;
    [ObservableProperty] ContactViewModel newItem = ContactViewModel.Default;

    public event EventHandler<ObservableCollection<ContactViewModel>>? OnMultipleResult;
    public event EventHandler<ContactViewModel>? OnSingleResult;
    public event EventHandler? OnZeroResults;
    public event EventHandler<ErrorRecord>? OnError;

    public ContactSearchViewModel()
    {
        _contactService.OnCacheRefreshed += UpdateAvailable;
        var initTask = _contactService.WaitForInit(_logging.LogError);
        initTask.WhenCompleted(() =>
        {
            UpdateAvailable(this, EventArgs.Empty);
        });
    }

    private void UpdateAvailable(object? sender, EventArgs e)
    {
        Available = [.._contactService.MyContacts.Select(ContactViewModel.Get)];
        foreach (var contactViewModel in Available)
        {
            contactViewModel.OnError += (sender, err) => OnError?.Invoke(sender, err);
            contactViewModel.Selected += (_, contact) => Select(contact);
        }
    }

    [RelayCommand]
    public void Create()
    {
        SearchText = SearchText.Trim();
        NewItem = new();
        if (SearchText.Contains("@"))
            NewItem.Email = SearchText;
        else if(SearchText.Contains(" "))
        {
            NewItem.FirstName = SearchText[..SearchText.IndexOf(' ')];
            NewItem.LastName = SearchText[(SearchText.IndexOf(' ') + 1)..];
        }
        ShowCreate = true;

        NewItem.Created += (_, vm) =>
        {
            Available.Add(vm);
            Select(vm);
            ShowCreate = false;
        };
    }


    [RelayCommand]
    public void Search()
    {
        var possible = Available.Where(contact =>
            contact.FullName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase) ||
            contact.Email.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)); 
        
        if (!possible.Any())
            OnZeroResults?.Invoke(this, EventArgs.Empty);
        
        switch (SelectionMode)
        {
            case SelectionMode.Multiple:
                AllSelected = [.. possible];
                IsSelected = AllSelected.Count > 0;
                OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.Single:
                Options = [.. possible];
                if (Options.Count == 0)
                {
                    ShowOptions = false;
                    Selected = ContactViewModel.Default;
                    IsSelected = false;
                    return;
                }

                if (Options.Count == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    SearchText = Selected.FullName;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = ContactViewModel.Default;
                IsSelected = false;
                return;
            default: return;
        }
    }

    [RelayCommand]
    public void ClearSelection()
    {
        IsSelected = false;
        Selected = ContactViewModel.Default;
        AllSelected = [];
        Options = [];
        ShowOptions = false;
        ShowCreate = false;
        NewItem = new();
    }

    public void Select(ContactViewModel e)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                Selected = Available.FirstOrDefault(contact => contact.Id == e.Id) ?? ContactViewModel.Default;
                IsSelected = string.IsNullOrEmpty(Selected.Id);
                Options = [];
                ShowOptions = false;
                SearchText = Selected.FullName;
                OnSingleResult?.Invoke(this, Selected);
                return;
            case SelectionMode.Multiple:
                var contact = Available.FirstOrDefault(cont => cont.Id == e.Id);
                if (contact is null) return;
                if (AllSelected.Contains(contact))
                    AllSelected.Remove(contact);
                else
                    AllSelected.Add(contact);

                IsSelected = AllSelected.Count > 0;
                if (IsSelected)
                    OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.None:
            default: return;
        }
    }

    async Task IAsyncSearchViewModel<ContactViewModel>.Search()
    {
        await Task.Run(Search);
    }
}
