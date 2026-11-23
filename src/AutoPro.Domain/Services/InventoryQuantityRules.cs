using AutoPro.Domain.Entities;

namespace AutoPro.Domain.Services;

public static class InventoryQuantityRules
{
    public static string? ValidateIssue(InventoryItem item, decimal quantity)
    {
        if (quantity <= 0)
            return "Quantity must be greater than zero.";
        if (!item.IsActive)
            return "That inventory part is inactive.";
        if (item.QuantityOnHand < quantity)
            return $"Insufficient stock for {item.PartNumber}. On hand: {item.QuantityOnHand}.";
        return null;
    }

    public static void ApplyIssue(InventoryItem item, decimal quantity)
    {
        item.QuantityOnHand -= quantity;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static string? ValidateReceipt(InventoryItem item, decimal quantity)
    {
        if (quantity <= 0)
            return "Received quantity must be greater than zero.";
        if (!item.IsActive)
            return $"Part {item.PartNumber} is inactive.";
        return null;
    }

    public static void ApplyReceipt(InventoryItem item, decimal quantity)
    {
        item.QuantityOnHand += quantity;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }
}