namespace AutoPro.Application.Contracts.Suppliers;

public record SupplierAdminListItemResponse(
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    int InventoryItemCount,
    bool IsActive,
    DateTime CreatedAtUtc);

public record SupplierAdminDetailResponse(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public record CreateSupplierRequest(
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive = true);

public record UpdateSupplierRequest(
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive);

public record SupplierOptionResponse(Guid Id, string Name);