using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.Jobs;

public record AddPurchaseLineRequest(
    PurchaseSource Source,
    Guid? InventoryItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string? VendorName);
