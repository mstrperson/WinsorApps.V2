using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinsorApps.Services.EventForms.Models;
public readonly record struct CateringMenuItem(string id, string name, double pricePerPerson, string category, bool isDeleted, bool fieldTripItem, int ordinal);
public readonly record struct CateringMenuCategory(string id, string name, bool isDeleted, bool fieldTripCategory, ImmutableArray<CateringMenuItem> items);

public readonly record struct CateringMenuSelection(string itemId, int quantity);

public readonly record struct DetailedCaterinMenuSelection(CateringMenuItem item, int quantity, double cost);

public readonly record struct NewCateringEvent(bool servers, bool cleanup,
    ImmutableArray<CateringMenuSelection> selectedItemIds, string budgetCodeId);

public readonly record struct CateringEvent(string id, bool servers, bool cleanup, double laborCost,
    ImmutableArray<DetailedCaterinMenuSelection> menuSelections, BudgetCode budgetCode, double cost);

public readonly record struct BudgetCode(string accountNumber, string name, string userId, string codeId);

public readonly record struct NewBudgetCode(string accountNumber, string name);
