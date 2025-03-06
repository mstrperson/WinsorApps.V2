using System.Collections.Immutable;
using System.Text.Json;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services
{
    public class BudgetCodeService : IAsyncInitService, ICacheService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public event EventHandler? OnCacheRefreshed;



        public BudgetCodeService(ApiService api, LocalLoggingService logging)
        {
            _api = api;
            _logging = logging;
        }

        public ImmutableArray<BudgetCode> BudgetCodes { get; private set; } = [];

        public BudgetCode? GetCode(string id) => BudgetCodes.Any(bc => bc.codeId == id) ? BudgetCodes.First(bc => bc.codeId == id) : null;

        public async Task<BudgetCode?> CreateNewBudgetCode(string accountNumber, string commonName, ErrorAction onError)
        {
            NewBudgetCode code = new NewBudgetCode(accountNumber, commonName);
            var result = await _api.SendAsync<NewBudgetCode, BudgetCode?>(HttpMethod.Post, "api/budget-codes", code, onError: onError);

            if (result.HasValue && !string.IsNullOrEmpty(result.Value.codeId) && !BudgetCodes.Any(code => code.codeId == result.Value.codeId))
            {
                BudgetCodes = BudgetCodes.Add(result.Value);
                OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            }
            return result;
        }

        public bool Started { get; private set; }

        public bool Ready { get; private set; }

        public double Progress { get; private set; } = 0;


        public async Task Initialize(ErrorAction  onError)
        {
            Started = true;
            while (!_api.Ready)
            {
                await Task.Delay(250);
            }
            Progress = 0;
            if (!LoadCache())
            {
                BudgetCodes = await _api.SendAsync<ImmutableArray<BudgetCode>?>(HttpMethod.Get, "api/budget-codes", onError: onError) ?? [];
                await SaveCache();
            }
            Progress = 1;
            Ready = true;
        }

        public async Task Refresh(ErrorAction onError)
        {
            var result = await _api.SendAsync<ImmutableArray<BudgetCode>?>(HttpMethod.Get, "api/budget-codes", onError: onError) ?? [];
            if (!result.SequenceEqual(BudgetCodes))
            {
                BudgetCodes = result;
                await SaveCache();
            }
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            if(!Started)
                await Initialize(onError);

            while(!Ready)
            {
                await Task.Delay(250);
            }
        }

        public string CacheFileName => ".budget-code.cache";
        public async Task SaveCache()
        {
            var json = JsonSerializer.Serialize(BudgetCodes);
            await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", json);
        }

        public bool LoadCache()
        {
            if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
                return false;

            try
            {
                var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
                BudgetCodes = JsonSerializer.Deserialize<ImmutableArray<BudgetCode>>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
