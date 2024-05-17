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
    public interface ICachedViewModel<T, TModel, TService> 
        where T : ObservableObject 
        where TModel : notnull
        where TService : IAsyncInitService
    {

        public abstract static ConcurrentBag<T> ViewModelCache { get; }

        public abstract static List<T> GetClonedViewModels(IEnumerable<TModel> models);

        public abstract static Task Initialize(TService service, ErrorAction onError);

        public abstract static T Get(TModel model);

        public T Clone();

        
    }
}
