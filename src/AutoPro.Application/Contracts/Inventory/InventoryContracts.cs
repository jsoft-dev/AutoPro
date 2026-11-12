namespace AutoPro.Application.Contracts.Inventory;

public record InventoryAdminListItemResponse(
    Guid Id,
    string PartNumber,
    string Name,
    decimal QuantityOnHand,
    decimal UnitPrice,
    bool IsActive,
    DateTime CreatedAtUtc);

public record InventoryAdminDetailResponse(
    Guid Id,
    string PartNumber,
    string Name,
    string? Notes,
    decimal QuantityOnHand,
    decimal UnitPrice,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public record CreateInventoryItemRequest(
    string PartNumber,
    string Name,
    string? Notes,
    decimal QuantityOnHand,
    decimal UnitPrice,
    bool IsActive = true);

public record UpdateInventoryItemRequest(
    string PartNumber,
    string Name,
    string? Notes,
    decimal QuantityOnHand,
    decimal UnitPrice,
    bool IsActive);

public record InventorySearchItemResponse(
    Guid Id,
    string PartNumber,
    string Name,
    decimal QuantityOnHand,
    decimal UnitPrice);

public record InventorySearchPageResponse(
    IReadOnlyList<InventorySearchItemResponse> Items,
    bool HasMore);
