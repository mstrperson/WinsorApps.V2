using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Concurrent;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels;

public interface ICachedViewModel<TViewModel, TModel, TService> 
    where TViewModel : ObservableObject
    where TModel : notnull
    where TService : IAsyncInitService
{

    public abstract static List<TViewModel> ViewModelCache { get; }

    public abstract static List<TViewModel> GetClonedViewModels(IEnumerable<TModel> models);

    public abstract static Task Initialize(TService service, ErrorAction onError);

    public abstract static TViewModel Get(TModel model);

    public TViewModel Clone();
}
public interface IModelCarrier<T, TModel> 
    where T : ObservableObject
    where TModel : class
{
    public Optional<TModel> Model { get; }

    abstract public static T Get(TModel model);
}
