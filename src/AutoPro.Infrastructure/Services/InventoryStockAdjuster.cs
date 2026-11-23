using AutoPro.Application.Inventory;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Services;

namespace AutoPro.Infrastructure.Services;

public class InventoryStockAdjuster : IInventoryStockAdjuster
{
    public string? TryIssue(InventoryItem item, decimal quantity)
    {
        var error = InventoryQuantityRules.ValidateIssue(item, quantity);
        if (error is not null)
            return error;
        InventoryQuantityRules.ApplyIssue(item, quantity);
        return null;
    }

    public string? TryReceipt(InventoryItem item, decimal quantity)
    {
        var error = InventoryQuantityRules.ValidateReceipt(item, quantity);
        if (error is not null)
            return error;
        InventoryQuantityRules.ApplyReceipt(item, quantity);
        return null;
    }

    public void ApplySellPriceFromCost(InventoryItem item, decimal unitCost)
    {
        if (unitCost < 0)
            return;
        item.UnitPrice = unitCost;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }
}