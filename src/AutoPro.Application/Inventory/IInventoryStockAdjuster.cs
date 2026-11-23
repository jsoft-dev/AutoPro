using AutoPro.Domain.Entities;

namespace AutoPro.Application.Inventory;

public interface IInventoryStockAdjuster
{
    string? TryIssue(InventoryItem item, decimal quantity);

    string? TryReceipt(InventoryItem item, decimal quantity);

    void ApplySellPriceFromCost(InventoryItem item, decimal unitCost);
}