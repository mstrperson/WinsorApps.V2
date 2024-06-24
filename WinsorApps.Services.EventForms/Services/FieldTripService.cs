using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services;

public class FieldTripService : IAsyncInitService
{
    private readonly ApiService _api;
    private readonly LocalLoggingService _logging;

    public FieldTripService(ApiService api, LocalLoggingService logging)
    {
        _api = api;
        _logging = logging;
    }



    public bool Started { get; protected set; }

    public bool Ready { get; protected set; }

    public double Progress { get; protected set; }

    public Task Initialize(ErrorAction onError)
    {
        throw new NotImplementedException();
    }

    public Task Refresh(ErrorAction onError)
    {
        throw new NotImplementedException();
    }

    public async Task WaitForInit(ErrorAction onError)
    {
        while (!Ready)
            await Task.Delay(250);
    }
}
