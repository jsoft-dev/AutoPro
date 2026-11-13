namespace AutoPro.Application.Contracts.Customers;

public record CustomerAdminDetailResponse(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    DateTime CreatedAtUtc,
    IReadOnlyList<CustomerAdminVehicleSummary> Vehicles);
