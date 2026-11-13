namespace AutoPro.Application.Contracts.Customers;

public record CustomerAdminListItemResponse(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    int VehicleCount,
    DateTime CreatedAtUtc);
