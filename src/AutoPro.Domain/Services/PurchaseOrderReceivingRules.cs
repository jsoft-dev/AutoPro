using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Services;

public static class PurchaseOrderReceivingRules
{
    public static string? ValidateCanReceiveAgainst(PurchaseOrder po)
    {
        if (po.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Cancelled)
            return "Cannot receive against a draft or cancelled purchase order.";
        if (po.Status is PurchaseOrderStatus.Closed)
            return "This purchase order is already fully received.";
        return null;
    }

    public static decimal Remaining(PurchaseOrderLine line) =>
        line.QuantityOrdered - line.QuantityReceived;

    public static string? ValidateReceiveQuantity(PurchaseOrderLine line, decimal receiveQty)
    {
        if (receiveQty <= 0)
            return "Received quantity must be greater than zero.";
        var remaining = Remaining(line);
        if (receiveQty > remaining)
            return $"Cannot receive {receiveQty} for part; only {remaining} remaining on PO line.";
        return null;
    }

    public static PurchaseOrderStatus RollUpStatus(IReadOnlyList<PurchaseOrderLine> lines)
    {
        if (lines.Count == 0)
            return PurchaseOrderStatus.Approved;

        var allReceived = lines.All(l => l.QuantityReceived >= l.QuantityOrdered);
        if (allReceived)
            return PurchaseOrderStatus.Closed;

        var anyReceived = lines.Any(l => l.QuantityReceived > 0);
        return anyReceived ? PurchaseOrderStatus.PartiallyReceived : PurchaseOrderStatus.Approved;
    }
}