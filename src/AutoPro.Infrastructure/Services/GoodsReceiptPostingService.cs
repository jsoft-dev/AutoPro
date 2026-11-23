using AutoPro.Application.Inventory;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;
using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Services;

public class GoodsReceiptPostingService(
    AutoProDbContext db,
    IInventoryStockAdjuster stockAdjuster) : IGoodsReceiptPostingService
{
    public async Task<string?> PostAsync(
        GoodsReceiptNote grn,
        bool updateSellingPriceFromUnitCost,
        Guid postedByUserId,
        CancellationToken ct)
    {
        if (grn.Status != GoodsReceiptNoteStatus.Draft)
            return "Only draft GRNs can be posted.";
        if (grn.Lines.Count == 0)
            return "Add at least one line before posting.";

        PurchaseOrder? po = null;
        if (grn.PurchaseOrderId is Guid poId)
        {
            po = await db.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == poId, ct);
            if (po is null)
                return "Linked purchase order was not found.";
            if (po.SupplierId != grn.SupplierId)
                return "GRN supplier must match the purchase order supplier.";

            var poError = PurchaseOrderReceivingRules.ValidateCanReceiveAgainst(po);
            if (poError is not null)
                return poError;
        }

        var itemIds = grn.Lines.Select(l => l.InventoryItemId).Distinct().ToList();
        var items = await db.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        foreach (var line in grn.Lines)
        {
            if (!items.TryGetValue(line.InventoryItemId, out var item))
                return "A line references a part that no longer exists.";

            var receiptError = InventoryQuantityRules.ValidateReceipt(item, line.QuantityReceived);
            if (receiptError is not null)
                return receiptError;

            if (po is not null)
            {
                if (line.PurchaseOrderLineId is not Guid poLineId)
                    return "Each line must reference a purchase order line when a PO is linked.";

                var poLine = po.Lines.FirstOrDefault(l => l.Id == poLineId);
                if (poLine is null)
                    return "A line references an invalid purchase order line.";
                if (poLine.InventoryItemId != line.InventoryItemId)
                    return "GRN line part does not match the purchase order line.";

                var qtyError = PurchaseOrderReceivingRules.ValidateReceiveQuantity(poLine, line.QuantityReceived);
                if (qtyError is not null)
                    return qtyError;
            }
        }

        foreach (var line in grn.Lines)
        {
            var item = items[line.InventoryItemId];
            InventoryQuantityRules.ApplyReceipt(item, line.QuantityReceived);

            if (updateSellingPriceFromUnitCost && line.UnitCost is decimal cost)
                stockAdjuster.ApplySellPriceFromCost(item, cost);

            if (po is null || line.PurchaseOrderLineId is not Guid poLineId)
                continue;

            var poLine = po.Lines.First(l => l.Id == poLineId);
            poLine.QuantityReceived += line.QuantityReceived;
        }

        if (po is not null)
        {
            po.Status = PurchaseOrderReceivingRules.RollUpStatus(po.Lines.ToList());
            po.UpdatedAtUtc = DateTime.UtcNow;
        }

        grn.Status = GoodsReceiptNoteStatus.Posted;
        grn.PostedAtUtc = DateTime.UtcNow;
        grn.PostedByUserId = postedByUserId;
        grn.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return null;
    }
}