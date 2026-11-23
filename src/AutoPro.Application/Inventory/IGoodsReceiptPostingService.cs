using AutoPro.Domain.Entities;

namespace AutoPro.Application.Inventory;

public interface IGoodsReceiptPostingService
{
    Task<string?> PostAsync(
        GoodsReceiptNote grn,
        bool updateSellingPriceFromUnitCost,
        Guid postedByUserId,
        CancellationToken ct);
}