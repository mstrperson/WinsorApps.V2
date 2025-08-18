using System.Collections.Immutable;

namespace WinsorApps.Services.EventForms.Models;

public record CreateCateringMenuItem(string name, double cost, bool availableForFieldTrip, DateTime effectiveDate = default, int ordinal = 0);
public record CateringMenuItem(string id, string name, double pricePerPerson, string category, bool isDeleted, bool fieldTripItem, int ordinal);

public record CreateCateringMenuCategory(string name, bool availableForFieldTrip);
public record CateringMenuCategory(string id, string name, bool isDeleted, bool fieldTripCategory, List<CateringMenuItem> items)
{
    public List<CateringMenuItem> AvailableItems => [.. items.Where(it => !it.isDeleted)];
    public List<CateringMenuItem> FieldTripAvailableItems => [.. AvailableItems.Where(it => it.fieldTripItem)];
}

public record CateringMenuSelection(string itemId, int quantity);

public record DetailedCateringMenuSelection(CateringMenuItem item, int quantity, double cost);

public record NewCateringEvent(bool servers, bool cleanup,
    List<CateringMenuSelection> selectedItemIds, string budgetCodeId);

public record CateringEvent(string id, bool servers, bool cleanup, double laborCost,
    List<DetailedCateringMenuSelection> menuSelections, BudgetCode budgetCode, double cost)
{
    public static readonly CateringEvent Empty = new("", false, false, 0, [], BudgetCode.None, 0);
}


public record BudgetCode(string accountNumber, string name, string userId, string codeId)
{
    public static readonly BudgetCode None = new("", "None", "", "");
}

public record NewBudgetCode(string accountNumber, string name);
