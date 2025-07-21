using AsyncAwaitBestPractices;
using System.Collections.Immutable;
using System.Text.Json;
using WinsorApps.Services.EventForms.Models;
using WinsorApps.Services.Global;
using WinsorApps.Services.Global.Services;

namespace WinsorApps.Services.EventForms.Services
{
    public class CateringMenuService :
        IAsyncInitService,
        ICacheService
    {
        private readonly ApiService _api;
        private readonly LocalLoggingService _logging;

        public event EventHandler? OnCacheRefreshed;

        public List<CateringMenuCategory> MenuCategories { get; private set; } = [];

        public List<CateringMenuCategory> AvailableCategories => [.. MenuCategories.Where(cat => !cat.isDeleted)];

        public void ClearCache() { if (File.Exists($"{_logging.AppStoragePath}{CacheFileName}")) File.Delete($"{_logging.AppStoragePath}{CacheFileName}"); }

        public CateringMenuService(LocalLoggingService logging, ApiService api)
        {
            _logging = logging;
            _api = api;

            _api.OnLoginSuccess += (_, _) => Initialize(_logging.LogError).SafeFireAndForget(e => e.LogException(_logging));

        }

        public bool Started { get; private set; }

        public bool Ready { get; private set; }

        public double Progress { get; private set; }

        public async Task Initialize(ErrorAction onError)
        {
            while (!_api.Ready)
                await Task.Delay(250);

            if (Started)
                return;

            Started = true;
            if (!LoadCache())
            {
                MenuCategories = await _api.SendAsync<List<CateringMenuCategory>?>(HttpMethod.Get, "api/events/catering/menu/all") ?? [];
                await SaveCache();
            }
            Progress = 1;
            Ready = true;
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        public async Task Refresh(ErrorAction onError)
        {
            MenuCategories = await _api.SendAsync<List<CateringMenuCategory>?>(HttpMethod.Get, "api/events/catering/menu/all") ?? [];
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
            await SaveCache();
        }

        public async Task WaitForInit(ErrorAction onError)
        {
            while (!Ready)
                await Task.Delay(250);
        }
        public string CacheFileName => ".catering-menu.cache";
        public async Task SaveCache()
        {
            var json = JsonSerializer.Serialize(MenuCategories);
            await File.WriteAllTextAsync($"{_logging.AppStoragePath}{CacheFileName}", json);
        }

        public bool LoadCache()
        {
            if (!File.Exists($"{_logging.AppStoragePath}{CacheFileName}"))
                return false;


            var cacheAge = DateTime.Now - File.GetCreationTime($"{_logging.AppStoragePath}{CacheFileName}");

            _logging.LogMessage(LocalLoggingService.LogLevel.Information,
                $"{CacheFileName} is {cacheAge.TotalDays:0.0} days old.");

            if (cacheAge.TotalDays > 14)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Information, "Deleting Aged Cache File.");
                File.Delete($"{_logging.AppStoragePath}{CacheFileName}");
                return false;
            }
            try
            {
                var json = File.ReadAllText($"{_logging.AppStoragePath}{CacheFileName}");
                MenuCategories = JsonSerializer.Deserialize<List<CateringMenuCategory>>(json) ?? [];
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task RefreshCache()
        {
            MenuCategories = await _api.SendAsync<List<CateringMenuCategory>?>(HttpMethod.Get, "api/events/catering/menu") ?? [];
            await SaveCache();
            OnCacheRefreshed?.Invoke(this, EventArgs.Empty);
        }

        public async Task DeleteItem(string itemId, ErrorAction onError)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;
            var result = await _api.SendAsync(HttpMethod.Delete, $"api/events/catering/items/{itemId}", onError: onError);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Delete Item {itemId} -- Result: {result}");

            var menu = MenuCategories.FirstOrDefault(cat => cat.items.Any(it => it.id == itemId));
            if (menu is null)
                return;

            var item = menu.items.First(it => it.id == itemId);
            menu.items.Remove(item);
            menu.items.Add(item with { isDeleted = true });
            await SaveCache();
        }

        public async Task<CateringMenuItem?> CreateItem(string menuId, CreateCateringMenuItem item, ErrorAction onError)
        {
            if (string.IsNullOrWhiteSpace(menuId) || item is null)
                return null;

            var result = await _api.SendAsync<CreateCateringMenuItem, CateringMenuItem?>(HttpMethod.Post, $"api/events/catering/menu/{menuId}/items", item, onError: onError);
            if (result is null)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to create catering item.");
                return null;
            }

            var menu = MenuCategories.FirstOrDefault(cat => cat.id == menuId);
            if (menu is null)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, $"Menu with ID {menuId} not found.");
                await RefreshCache();
                return result;
            }

            menu.items.Add(result);
            await SaveCache();
            return result;
        }

        public async Task<CateringMenuItem?> UpdateItem(string itemId, CreateCateringMenuItem item, ErrorAction onError)
        {
            if (string.IsNullOrWhiteSpace(itemId) || item is null)
                return null;
            var result = await _api.SendAsync<CreateCateringMenuItem, CateringMenuItem?>(HttpMethod.Put, $"api/events/catering/items/{itemId}", item, onError: onError);
            if (result is null)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to update catering item.");
                return null;
            }
            var menu = MenuCategories.FirstOrDefault(cat => cat.items.Any(it => it.id == itemId));
            if (menu is null)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, $"Menu with item ID {itemId} not found.");
                await RefreshCache();
                return result;
            }
            var existingItem = menu.items.First(it => it.id == itemId);
            menu.items.Remove(existingItem);
            menu.items.Add(existingItem with { isDeleted = true });
            menu.items.Add(result);
            await SaveCache();
            return result;
        }

        public async Task<CateringMenuCategory?> CreateCategory(CreateCateringMenuCategory category, ErrorAction onError)
        {
            if (category is null)
                return null;
            var result = await _api.SendAsync<CreateCateringMenuCategory, CateringMenuCategory?>(HttpMethod.Post, "api/events/catering/menu", category, onError: onError);
            if (result is null)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to create catering category.");
                return null;
            }
            MenuCategories.Add(result);
            await SaveCache();
            return result;
        }

        public async Task DeleteCategory(string menuId, ErrorAction onError)
        {
            if (string.IsNullOrWhiteSpace(menuId))
                return;
            var result = await _api.SendAsync(HttpMethod.Delete, $"api/events/catering/menu/{menuId}", onError: onError);
            _logging.LogMessage(LocalLoggingService.LogLevel.Information, $"Delete Menu {menuId} -- Result: {result}");
            var menu = MenuCategories.FirstOrDefault(cat => cat.id == menuId);
            if (menu is null)
                return;
            MenuCategories.Remove(menu);
            MenuCategories.Add(menu with { isDeleted = true });
            await SaveCache();
        }

        public async Task<CateringMenuCategory?> MoveItemOrdinal(string menuId, string itemId, int ordinal, ErrorAction onError)
        {
            var result = await _api.SendAsync<CateringMenuItem?>(HttpMethod.Put,
                $"api/events/catering/items/{itemId}/move-to/{ordinal}", onError: onError);
            if(result is null)
            {
                _logging.LogMessage(LocalLoggingService.LogLevel.Error, "Failed to move catering item ordinal.");
                return null;
            }

            await RefreshCache();
            var menu = MenuCategories.FirstOrDefault(cat => cat.id == menuId);
            return menu;
        }
    }
}
