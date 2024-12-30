// See https://aka.ms/new-console-template for more information

using WinsorApps.Services.Global.Models;
using WinsorApps.Services.Global.Services;
using WinsorApps.Services.Helpdesk.Services;

#region Service Declarations
LocalLoggingService logging = new();
SavedCredential sc = new();
ApiService api = new(logging, sc);
RegistrarService registrar = new RegistrarService(api, logging);
DeviceService deviceService = new(api, logging);
CheqroomService cheqroomService = new(api, logging);
JamfService jamfService = new(api, logging);
ServiceCaseService serviceCaseService = new(api, logging);
#endregion

// Do Login Stuff
await api.Initialize(OnError);
int retryCount = 0;
while (!api.Ready && retryCount < 10)
{
    await api.Login("jcox@winsor.edu", "!-8L49snDyYvcNJe29a.p!N4ka3wf", OnError);
    if (!api.Ready)
        retryCount++;
}

if (!api.Ready)
    return;

// Initialize Registrar Service

DateTime start = DateTime.Now;
Console.WriteLine("Initializing");

await Task.WhenAll(
    registrar.Initialize(OnError),
    deviceService.Initialize(OnError), 
    jamfService.Initialize(OnError),
    cheqroomService.Initialize(OnError),
    serviceCaseService.Initialize(OnError));

var time = DateTime.Now - start;

Console.WriteLine($"Initializing took {time:hh:mm:ss}");

void OnError(ErrorRecord err)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(err.type);
    Console.WriteLine(err.error);
    Console.ResetColor();
}