using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    IEmptyViewModel<ContactViewModel>,
    ICachedViewModel<ContactViewModel, Contact, ContactService>,
    IErrorHandling,
    ISelectable<ContactViewModel>
    
{
    private readonly ContactService contactService = ServiceHelper.GetService<ContactService>();

    [ObservableProperty] string id = "";
    [ObservableProperty] string firstName = "";
    [ObservableProperty] string fullName = "";
    [ObservableProperty] string lastName = "";
    [ObservableProperty] string email = "";
    [ObservableProperty] string phone = "";
    [ObservableProperty] UserViewModel associatedUser = IEmptyViewModel<UserViewModel>.Empty;
    [ObservableProperty] bool isPublic;
    [ObservableProperty] bool isSelected;

    public event EventHandler<ErrorRecord>? OnError;
    public event EventHandler<ContactViewModel>? Selected;

    [RelayCommand]
    public async Task Create()
    {
        var newContact = new NewContact(FirstName, LastName, Email, Phone, IsPublic);
        await contactService.CreateNewContact(newContact, OnError.DefaultBehavior(this));
    }

    public static ConcurrentBag<ContactViewModel> ViewModelCache { get; private set; } = [];
    

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

        if(!string.IsNullOrEmpty(model.associatedUserId))
        {
            var user = UserViewModel.ViewModelCache.FirstOrDefault(u => u.Id == model.associatedUserId);
            if(user is not null) 
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

    public void Select()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
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

    [ObservableProperty] ImmutableArray<ContactViewModel> available = [];
    [ObservableProperty] ImmutableArray<ContactViewModel> allSelected = [];
    [ObservableProperty] ImmutableArray<ContactViewModel> options = [];
    [ObservableProperty] ContactViewModel selected = IEmptyViewModel<ContactViewModel>.Empty;
    [ObservableProperty] SelectionMode selectionMode = SelectionMode.Single;
    [ObservableProperty] string searchText = "";
    [ObservableProperty] bool isSelected;
    [ObservableProperty] bool showOptions;

    public event EventHandler<ImmutableArray<ContactViewModel>>? OnMultipleResult;
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
        Available = ContactViewModel.GetClonedViewModels(_contactService.MyContacts).ToImmutableArray();
        foreach (var contactViewModel in Available)
        {
            contactViewModel.OnError += (sender, err) => OnError?.Invoke(sender, err);
            contactViewModel.Selected += (_, contact) => Select(contact);
        }
    }

    public void Search()
    {
        var possible = Available.Where(contact =>
            contact.FullName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)); 
        
        if (!possible.Any())
            OnZeroResults?.Invoke(this, EventArgs.Empty);
        
        switch (SelectionMode)
        {
            case SelectionMode.Multiple:
                AllSelected = [.. possible];
                IsSelected = AllSelected.Length > 0;
                OnMultipleResult?.Invoke(this, AllSelected);
                return;
            case SelectionMode.Single:
                Options = [.. possible];
                if (Options.Length == 0)
                {
                    ShowOptions = false;
                    Selected = IEmptyViewModel<ContactViewModel>.Empty;
                    IsSelected = false;
                    return;
                }

                if (Options.Length == 1)
                {
                    ShowOptions = false;
                    Selected = Options.First();
                    IsSelected = true;
                    SearchText = Selected.FullName;
                    OnSingleResult?.Invoke(this, Selected);
                    return;
                }

                ShowOptions = true;
                Selected = IEmptyViewModel<ContactViewModel>.Empty;
                IsSelected = false;
                return;
            default: return;
        }
    }

    public void Select(ContactViewModel e)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                Selected = Available.FirstOrDefault(contact => contact.Id == e.Id) ?? IEmptyViewModel<ContactViewModel>.Empty;
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
                    AllSelected = AllSelected.Remove(contact);
                else
                    AllSelected = AllSelected.Add(contact);

                IsSelected = AllSelected.Length > 0;
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
