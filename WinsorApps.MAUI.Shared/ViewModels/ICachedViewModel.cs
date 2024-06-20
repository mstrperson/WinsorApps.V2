using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.MAUI.Shared.ViewModels
{
    public interface ICachedViewModel<TViewModel, TModel, TService> 
        where TViewModel : ObservableObject
        where TModel : notnull
        where TService : IAsyncInitService
    {

        public abstract static ConcurrentBag<TViewModel> ViewModelCache { get; }

        public abstract static List<TViewModel> GetClonedViewModels(IEnumerable<TModel> models);

        public abstract static Task Initialize(TService service, ErrorAction onError);

        public abstract static TViewModel Get(TModel model);

        public TViewModel Clone();        
    }
}
