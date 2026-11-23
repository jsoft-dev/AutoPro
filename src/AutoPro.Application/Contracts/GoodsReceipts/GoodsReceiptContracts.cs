using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.GoodsReceipts;

public record GoodsReceiptAdminListItemResponse(
    Guid Id,
    string GrnNumber,
    Guid SupplierId,
    string SupplierName,
    Guid? PurchaseOrderId,
    string? PurchaseOrderNumber,
    GoodsReceiptNoteStatus Status,
    DateTime ReceivedAtUtc,
    int LineCount,
    DateTime CreatedAtUtc,
    DateTime? PostedAtUtc);

public record GoodsReceiptLineResponse(
    Guid Id,
    Guid InventoryItemId,
    string PartNumber,
    string PartName,
    Guid? PurchaseOrderLineId,
    decimal QuantityReceived,
    decimal? UnitCost,
    string? Notes);

public record GoodsReceiptAdminDetailResponse(
    Guid Id,
    string GrnNumber,
    Guid SupplierId,
    string SupplierName,
    Guid? PurchaseOrderId,
    string? PurchaseOrderNumber,
    GoodsReceiptNoteStatus Status,
    DateTime ReceivedAtUtc,
    string? SupplierReference,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? PostedAtUtc,
    IReadOnlyList<GoodsReceiptLineResponse> Lines);

public record GoodsReceiptLineWriteRequest(
    Guid InventoryItemId,
    Guid? PurchaseOrderLineId,
    decimal QuantityReceived,
    decimal? UnitCost,
    string? Notes);

public record CreateGoodsReceiptRequest(
    Guid SupplierId,
    Guid? PurchaseOrderId,
    DateTime ReceivedAtUtc,
    string? SupplierReference,
    string? Notes,
    IReadOnlyList<GoodsReceiptLineWriteRequest> Lines);

public record UpdateGoodsReceiptRequest(
    Guid SupplierId,
    Guid? PurchaseOrderId,
    DateTime ReceivedAtUtc,
    string? SupplierReference,
    string? Notes,
    IReadOnlyList<GoodsReceiptLineWriteRequest> Lines);

public record PostGoodsReceiptRequest(bool UpdateSellingPriceFromUnitCost = false);