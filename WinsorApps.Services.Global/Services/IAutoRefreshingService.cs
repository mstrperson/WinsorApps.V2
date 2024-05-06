using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinsorApps.Services.Global.Services
{
    public interface IAutoRefreshingService
    {
        public TimeSpan RefreshInterval { get; }

        public bool Refreshing { get; }

        public Task RefreshInBackground(CancellationToken token, ErrorAction onError);
    }
}
