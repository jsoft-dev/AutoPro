using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.PurchaseOrders;

public record PurchaseOrderAdminListItemResponse(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    PurchaseOrderStatus Status,
    int LineCount,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc);

public record PurchaseOrderLineResponse(
    Guid Id,
    Guid InventoryItemId,
    string PartNumber,
    string PartName,
    decimal QuantityOrdered,
    decimal QuantityReceived,
    decimal QuantityRemaining,
    decimal? UnitCost,
    string? Notes);

public record PurchaseOrderAdminDetailResponse(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    PurchaseOrderStatus Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ApprovedAtUtc,
    IReadOnlyList<PurchaseOrderLineResponse> Lines);

public record PurchaseOrderLineWriteRequest(
    Guid InventoryItemId,
    decimal QuantityOrdered,
    decimal? UnitCost,
    string? Notes);

public record CreatePurchaseOrderRequest(
    Guid SupplierId,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineWriteRequest> Lines);

public record UpdatePurchaseOrderRequest(
    Guid SupplierId,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineWriteRequest> Lines);

public record PurchaseOrderOptionResponse(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    PurchaseOrderStatus Status);